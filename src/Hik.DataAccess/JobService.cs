using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;
using NLog;

namespace Hik.DataAccess
{
    public class JobService : IJobService
    {
        private readonly IUnitOfWorkFactory factory;
        private static readonly IMapper mapper = new MapperConfiguration(configureAutoMapper).CreateMapper();
        protected readonly Logger logger = LogManager.GetCurrentClassLogger();

        public JobService(IUnitOfWorkFactory factory)
        {
            this.factory = factory;
        }

        public async Task CreateJobInstanceAsync(HikJob job)
        {
            using var unitOfWork = this.factory.CreateUnitOfWork();
            var jobRepo = unitOfWork.GetRepository<HikJob>();
            await jobRepo.AddAsync(job);
            await unitOfWork.SaveChangesAsync();
        }

        public async Task LogExceptionToDbAsync(int jobId, string message, string callStack, uint? errorCode = null)
        {
            using var unitOfWork = this.factory.CreateUnitOfWork();
            var jobRepo = unitOfWork.GetRepository<ExceptionLog>();

            await jobRepo.AddAsync(new ExceptionLog
            {
                CallStack = callStack,
                JobId = jobId,
                Message = message,
                HikErrorCode = errorCode
            });
            await unitOfWork.SaveChangesAsync();
        }

        public async Task<JobTrigger> GetOrCreateJobTriggerAsync(string triggerKey)
        {
            var jobNameParts = triggerKey.Split(".");
            var group = jobNameParts[0];
            var key = jobNameParts[1];

            using var unitOfWork = this.factory.CreateUnitOfWork();
            var repo = unitOfWork.GetRepository<JobTrigger>();
            var jobTrigger = await repo.FindByAsync(x => x.TriggerKey == key && x.Group == group);
            if (jobTrigger == null)
            {
                jobTrigger = await repo.AddAsync(new JobTrigger { TriggerKey = key, Group = group });
                await unitOfWork.SaveChangesAsync();
            }
            return jobTrigger;
        }

        public async Task SaveJobResultAsync(HikJob job)
        {
            using var unitOfWork = factory.CreateUnitOfWork();
            var jobRepo = unitOfWork.GetRepository<HikJob>();
            job.Finished = DateTime.Now;
            jobRepo.Update(job);

            if (job.Success)
            {
                var triggerRepo = unitOfWork.GetRepository<JobTrigger>();
                var trigger = await triggerRepo.FindByAsync(x => x.Id == job.JobTriggerId);
                trigger.LastSync = job.Started;
            }

            await unitOfWork.SaveChangesAsync(job);
        }

        public async Task<List<MediaFile>> SaveFilesAsync(HikJob job, IReadOnlyCollection<MediaFileDTO> files)
        {
            using var unitOfWork = factory.CreateUnitOfWork();

            var mediaFiles = new List<MediaFile>();
            var mediaFileDuration = new List<DownloadDuration>();
            foreach (var item in files)
            {
                MediaFile file = mapper.Map<MediaFile>(item);
                file.JobTriggerId = job.JobTriggerId;
                if (item.DownloadDuration != null)
                {
                    DownloadDuration duration = mapper.Map<DownloadDuration>(item);
                    duration.MediaFile = file;
                    mediaFileDuration.Add(duration);
                }
                mediaFiles.Add(file);
            }

            await AddEntities(mediaFiles, unitOfWork);
            await AddEntities(mediaFileDuration, unitOfWork);
            await unitOfWork.SaveChangesAsync(job);
            return mediaFiles;
        }

        public async Task UpdateDailyStatisticsAsync(HikJob job, IReadOnlyCollection<MediaFileDTO> files)
        {
            using var unitOfWork = factory.CreateUnitOfWork();

            var from = files.Min(x => x.Date).Date;
            var to = files.Max(x => x.Date).Date;

            var dailyStat = (await GetDailyStatisticSafe(job.JobTriggerId, from, to, unitOfWork)).ToDictionary(k => k.Period, v => v);

            var group = files.GroupBy(x => x.Date.Date)
                .Select(x => new { Date = x.Key, Count = x.Count(), Size = x.Sum(p => p.Size), Duration = x.Sum(p => p.Duration) });

            foreach (var item in group)
            {
                var day = dailyStat[item.Date];
                day.FilesCount += item.Count;
                day.FilesSize += item.Size;
                day.TotalDuration += item.Duration;
            }

            await unitOfWork.SaveChangesAsync(job);
        }

        public async Task SaveDownloadHistoryFilesAsync(HikJob job, IReadOnlyCollection<MediaFile> files)
        {
            using var unitOfWork = factory.CreateUnitOfWork();
            var entities = files.Select(x => new DownloadHistory { MediaFileId = x.Id }).ToList();

            await AddEntities(entities, unitOfWork);
            await unitOfWork.SaveChangesAsync(job);
        }

        public async Task DeleteObsoleteJobsAsync(string[] triggers, DateTime to)
        {
            logger.Info("DeleteObsoleteJobsAsync");
            using (var unitOfWork = factory.CreateUnitOfWork())
            {
                var triggerRepo = unitOfWork.GetRepository<JobTrigger>();
                var jobRepo = unitOfWork.GetRepository<HikJob>();
                var filesRepo = unitOfWork.GetRepository<MediaFile>();

                foreach (var trigger in triggers)
                {
                    var parts = trigger.Split(".");
                    logger.Info($"Try cleanup {trigger}");
                    var jobTrigger = await triggerRepo.FindByAsync(x => x.Group == parts[0] && x.TriggerKey == parts[1]);
                    if (jobTrigger != null)
                    {
                       logger.Info($"{trigger} found, Id = {jobTrigger.Id}, To = {to}");

                       var files = await filesRepo.FindManyAsync(x => x.JobTriggerId == jobTrigger.Id && x.Date <= to,
                            x => x.DownloadDuration,
                            x => x.DownloadHistory);
                        filesRepo.RemoveRange(files);
                        logger.Info($"{trigger} files cleared");

                        var jobs = await jobRepo.FindManyAsync(x => x.JobTriggerId == jobTrigger.Id && x.PeriodEnd <= to, x => x.DownloadedFiles);
                        jobRepo.RemoveRange(jobs);
                        logger.Info($"{trigger} jobs cleared");
                    }
                }

                await unitOfWork.SaveChangesAsync();
                logger.Info($"Cleanup done");
            }
        }

        private static async Task<List<DailyStatistic>> GetDailyStatisticSafe(int triggerId, DateTime from, DateTime to, IUnitOfWork unitOfWork)
        {
            var repo = unitOfWork.GetRepository<DailyStatistic>();
            var daily = await repo.FindManyAsync(x => x.JobTriggerId == triggerId && x.Period >= from && x.Period <= to);

            var newItems = new List<DailyStatistic>();
            do
            {
                if (!daily.Any(d => d.Period == from))
                {
                    newItems.Add(new DailyStatistic
                        {
                            JobTriggerId = triggerId,
                            Period = from,
                            FilesCount = 0,
                            FilesSize = 0,
                            TotalDuration = 0
                        });
                }
                from = from.AddDays(1);
            } while (from <= to);

            await repo.AddRangeAsync(newItems);
            daily.AddRange(newItems);
            return daily;
        }

        private static Task AddEntities<TEntity>(List<TEntity> entities, IUnitOfWork unitOfWork)
            where TEntity : class
        {
            if (entities != null && entities.Any())
            {
                var repo = unitOfWork.GetRepository<TEntity>();
                return repo.AddRangeAsync(entities);
            }

            return Task.CompletedTask;
        }

        static void configureAutoMapper(IMapperConfigurationExpression x)
        {
            x.AddProfile<AutoMapperProfile>();
        }
    }
}
