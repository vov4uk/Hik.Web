using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;
using Serilog;

namespace Hik.DataAccess
{
    public class HikDatabase : IHikDatabase
    {
        private readonly IUnitOfWorkFactory factory;
        public static readonly IMapper Mapper = new MapperConfiguration(configureAutoMapper).CreateMapper();
        protected readonly ILogger logger;

        public HikDatabase(IUnitOfWorkFactory factory, ILogger logger)
        {
            this.factory = factory;
            this.logger = logger;
        }

        public async Task<HikJob> CreateJobInstanceAsync(HikJob job)
        {
            using var unitOfWork = this.factory.CreateUnitOfWork();
            var jobRepo = unitOfWork.GetRepository<HikJob>();
            var newJob = await jobRepo.AddAsync(job);
            await unitOfWork.SaveChangesAsync();
            return newJob;
        }

        public async Task LogExceptionToAsync(int jobId, string message)
        {
            using var unitOfWork = this.factory.CreateUnitOfWork();
            var jobRepo = unitOfWork.GetRepository<ExceptionLog>();

            await jobRepo.AddAsync(new ExceptionLog
            {
                JobId = jobId,
                Message = message,
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
            using (var unitOfWork = factory.CreateUnitOfWork())
            {
                var jobRepo = unitOfWork.GetRepository<HikJob>();
                job.Finished = DateTime.Now;
                jobRepo.Update(job);

                if (job.Success)
                {
                    var triggerRepo = unitOfWork.GetRepository<JobTrigger>();
                    var trigger = await triggerRepo.FindByAsync(x => x.Id == job.JobTriggerId);
                    trigger.LastSync = job.LatestFileEndDate ?? job.Started;
                    trigger.LastExecutedJobId = job.Id;
                    if (job.PeriodEnd != null)
                    {
                        trigger.LastSuccessJobId = job.Id;
                    }
                    triggerRepo.Update(trigger);
                }

                await unitOfWork.SaveChangesAsync();
            }
        }

        public async Task UpdateJobAsync(HikJob job)
        {
            using (var unitOfWork = factory.CreateUnitOfWork())
            {
                var jobRepo = unitOfWork.GetRepository<HikJob>();
                jobRepo.Update(job);
                await unitOfWork.SaveChangesAsync();
            }
        }

        public async Task<List<MediaFile>> SaveFilesAsync(HikJob job, IReadOnlyCollection<MediaFileDto> files)
        {
            using var unitOfWork = factory.CreateUnitOfWork();

            var mediaFiles = new List<MediaFile>();
            var mediaFileDuration = new List<DownloadDuration>();
            foreach (var item in files)
            {
                MediaFile file = Mapper.Map<MediaFile>(item);
                file.JobTriggerId = job.JobTriggerId;
                if (item.DownloadDuration != null)
                {
                    DownloadDuration duration = new()
                    {
                        MediaFile = file,
                        Duration = item.DownloadDuration,
                        Started = item.DownloadStarted,
                    };
                    mediaFileDuration.Add(duration);
                }
                mediaFiles.Add(file);
            }

            await AddEntities(mediaFiles, unitOfWork);
            await AddEntities(mediaFileDuration, unitOfWork);
            await unitOfWork.SaveChangesAsync(job);
            return mediaFiles;
        }

        public async Task UpdateDailyStatisticsAsync(int jobTriggerId, IReadOnlyCollection<MediaFileDto> files)
        {
            using (var unitOfWork = factory.CreateUnitOfWork())
            {
                IBaseRepository<DailyStatistic> repo = unitOfWork.GetRepository<DailyStatistic>();

                var from = files.Min(x => x.Date).Date;
                var to = files.Max(x => x.Date).Date;

                Dictionary<DateTime, DailyStatistic> dailyStat = await GetDailyStatisticSafe(jobTriggerId, from, to, repo);

                var group = files.GroupBy(x => x.Date.Date)
                    .Select(x => new { Date = x.Key, Count = x.Count(), Size = x.Sum(p => p.Size), Duration = x.Sum(p => p.Duration) });

                foreach (var item in group)
                {
                    var day = dailyStat[item.Date];
                    day.FilesCount += item.Count;
                    day.FilesSize += item.Size;
                    day.TotalDuration += item.Duration;
                }

                await unitOfWork.SaveChangesAsync();
            }
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
            using (var unitOfWork = factory.CreateUnitOfWork())
            {
                var triggerRepo = unitOfWork.GetRepository<JobTrigger>();
                var jobRepo = unitOfWork.GetRepository<HikJob>();
                var filesRepo = unitOfWork.GetRepository<MediaFile>();

                foreach (var trigger in triggers)
                {
                    var parts = trigger.Split(".");
                    logger.Information("Try cleanup {trigger}", trigger);
                    var jobTrigger = await triggerRepo.FindByAsync(x => x.Group == parts[0] && x.TriggerKey == parts[1]);
                    if (jobTrigger != null)
                    {
                       logger.Information("{trigger} found, Id = {Id}, To = {to}", trigger, jobTrigger.Id, to);

                       var files = await filesRepo.FindManyAsync(x => x.JobTriggerId == jobTrigger.Id && x.Date <= to,
                            x => x.DownloadDuration,
                            x => x.DownloadHistory);
                        filesRepo.RemoveRange(files);
                        await unitOfWork.SaveChangesAsync();
                        logger.Information("{trigger} files cleared", trigger);

                        var jobs = await jobRepo.FindManyAsync(x => x.JobTriggerId == jobTrigger.Id && x.Finished <= to, x => x.DownloadedFiles);
                        jobRepo.RemoveRange(jobs);
                        await unitOfWork.SaveChangesAsync();
                        logger.Information("{trigger} jobs cleared", trigger);
                    }
                }

                logger.Information("Cleanup done");
            }
        }

        private static async Task<Dictionary<DateTime, DailyStatistic>> GetDailyStatisticSafe(int triggerId, DateTime from, DateTime to, IBaseRepository<DailyStatistic> repo)
        {
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
            return daily.ToDictionary(k => k.Period, v => v);
        }

        private static async Task AddEntities<TEntity>(List<TEntity> entities, IUnitOfWork unitOfWork)
            where TEntity : class, IEntity
        {
            if (entities != null && entities.Any())
            {
                var repo = unitOfWork.GetRepository<TEntity>();
                await repo.AddRangeAsync(entities);
            }
        }

        static void configureAutoMapper(IMapperConfigurationExpression x)
        {
            x.AddProfile<HikMappingProfile>();
        }
    }
}
