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
    public class JobService
    {
        private readonly IUnitOfWorkFactory factory;
        private readonly HikJob job;
        private static readonly IMapper mapper = new MapperConfiguration(configureAutoMapper).CreateMapper();
        protected readonly Logger logger = LogManager.GetCurrentClassLogger();

        public JobService(IUnitOfWorkFactory factory, HikJob job)
        {
            this.factory = factory;
            this.job = job;
        }

        public async Task SaveJobResultAsync()
        {
            using var unitOfWork = factory.CreateUnitOfWork();
            var jobRepo = unitOfWork.GetRepository<HikJob>();
            job.Finished = DateTime.Now;
            await jobRepo.UpdateAsync(job);

            if (job.Success)
            {
                var triggerRepo = unitOfWork.GetRepository<JobTrigger>();
                var trigger = await triggerRepo.FindByAsync(x => x.Id == job.JobTriggerId);
                trigger.LastSync = job.Started;
            }

            await unitOfWork.SaveChangesAsync(job);
        }

        public async Task<List<MediaFile>> SaveFilesAsync(IReadOnlyCollection<MediaFileDTO> files)
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

        public async Task UpdateDailyStatisticsAsync(IReadOnlyCollection<MediaFileDTO> files)
        {
            using var unitOfWork = factory.CreateUnitOfWork();

            var from = files.Min(x => x.Date).Date;
            var to = files.Max(x => x.Date).Date;

            var dailyStat = (await GetDailyStatisticSafe(job.JobTriggerId, from, to, unitOfWork)).ToDictionary(k => k.Period, v => v);

            var group = files.GroupBy(x => x.Date.Date)
                .Select(x => new { date = x.Key, cnt = x.Count(), size = x.Sum(p => p.Size), dur = x.Sum(p => p.Duration) });

            foreach (var item in group)
            {
                var day = dailyStat[item.date];
                day.FilesCount += item.cnt;
                day.FilesSize += item.size;
                day.TotalDuration += item.dur;
            }

            await unitOfWork.SaveChangesAsync(job);
        }

        public async Task SaveHistoryFilesAsync<T>(IReadOnlyCollection<MediaFile> files)
            where T: class, IHistory, new()
        {
            using var unitOfWork = factory.CreateUnitOfWork();
            var entities = files.Select(x => new T { MediaFileId = x.Id }).ToList();

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
                var downloadRepo = unitOfWork.GetRepository<DownloadHistory>();

                foreach (var trigger in triggers)
                {
                    var parts = trigger.Split(".");
                    logger.Info($"Try cleanup {trigger}");
                    var jobTrigger = await triggerRepo.FindByAsync(x => x.Group == parts[0] && x.TriggerKey == parts[1]);
                    if (jobTrigger != null)
                    {
                        logger.Info($"{trigger} found, Id = {jobTrigger.Id}, To = {to.ToLongDateString()}");

                        var jobs = await jobRepo.FindManyAsync(x => x.JobTriggerId == jobTrigger.Id && x.PeriodEnd <= to);
                        var jobIds = jobs.Select(x => x.Id).Distinct();

                        var files = await downloadRepo.FindManyAsync(x => jobIds.Contains(x.JobId));
                        var fileIds = files.Select(x => x.Id).Distinct();

                        await downloadRepo.DeleteAsync(x => jobIds.Contains(x.JobId));
                        logger.Info($"{trigger} downloadHistory cleared");

                        await filesRepo.DeleteAsync(x => fileIds.Contains(x.Id));
                        logger.Info($"{trigger} files cleared");

                        await jobRepo.DeleteAsync(x => jobIds.Contains(x.Id));
                        logger.Info($"{trigger} jobs cleared");
                    }
                }

                unitOfWork.SaveChanges();
                logger.Info($"Cleanup done");
            }
        }

        private async Task<List<DailyStatistic>> GetDailyStatisticSafe(int triggerId, DateTime from, DateTime to, IUnitOfWork unitOfWork)
        {
            var repo = unitOfWork.GetRepository<DailyStatistic>();
            var daily = await repo.FindManyAsync(x => x.JobTriggerId == triggerId && x.Period >= from && x.Period <= to);

            var newItems = new List<DailyStatistic>();
            do
            {
                if (!daily.Any(d => d.Period == from))
                {
                    newItems.Add(
                        new DailyStatistic
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

        private Task AddEntities<TEntity>(List<TEntity> entities, IUnitOfWork unitOfWork)
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
