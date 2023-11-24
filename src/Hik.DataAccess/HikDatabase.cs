using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AutoMapper;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;
using Microsoft.EntityFrameworkCore;
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

        public void LogExceptionTo(int jobId, string message)
        {
            using (var unitOfWork = this.factory.CreateUnitOfWork())
            {
                var jobRepo = unitOfWork.GetRepository<ExceptionLog>();

                jobRepo.Add(new ExceptionLog
                {
                    JobId = jobId,
                    Message = message,
                    Created = DateTime.Now
                });
                unitOfWork.SaveChanges();
            }
        }

        public async Task<JobTrigger> GetJobTriggerAsync(string group, string key)
        {
            JobTrigger jobTrigger = null;
            using (var unitOfWork = this.factory.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
            {
                var repo = unitOfWork.GetRepository<JobTrigger>();
                jobTrigger = await repo.FindByAsync(x => x.TriggerKey == key && x.Group == group);
            }
            return jobTrigger;
        }

        public async Task<JobTrigger[]> GetJobTriggersAsync(int[] triggerIds)
        {
            using (var unitOfWork = this.factory.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
            {
                var repo = unitOfWork.GetRepository<JobTrigger>();
                var jobTriggers = await repo.FindManyAsync(x => triggerIds.Contains(x.Id));
                return jobTriggers.ToArray();
            }
        }

        public HikJob CreateJob(HikJob job)
        {
            using (var uow = factory.CreateUnitOfWork())
            {
                var jobRepo = uow.GetRepository<HikJob>();
                var jobInstance = jobRepo.Add(job);
                uow.SaveChanges();
                return jobInstance;
            }
        }

        public void UpdateJobTrigger(JobTrigger trigger)
        {
            using (var unitOfWork = factory.CreateUnitOfWork())
            {
                var triggerRepo = unitOfWork.GetRepository<JobTrigger>();
                triggerRepo.Update(trigger);
            }
        }

        public void UpdateJob(HikJob job)
        {
            using (var unitOfWork = factory.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
            {
                var repo = unitOfWork.GetRepository<HikJob>();
                repo.Update(job);
            }
        }


        public List<MediaFile> SaveFiles(HikJob job, IReadOnlyCollection<MediaFileDto> files)
        {
            var mediaFiles = new List<MediaFile>();

            foreach (var item in files)
            {
                MediaFile file = Mapper.Map<MediaFile>(item);
                file.JobTriggerId = job.JobTriggerId;
                file.JobId = job.Id;

                mediaFiles.Add(file);
            }

            using (var unitOfWork = factory.CreateUnitOfWork())
            {
                var repo = unitOfWork.GetRepository<MediaFile>();
                repo.AddRange(mediaFiles);
                unitOfWork.SaveChanges();
            }
            return mediaFiles;
        }

        public void SaveFile(HikJob job, MediaFileDto item)
        {
            MediaFile file = Mapper.Map<MediaFile>(item);
            file.JobTriggerId = job.JobTriggerId;
            file.JobId = job.Id;

            using (var unitOfWork = factory.CreateUnitOfWork())
            {
                var repo = unitOfWork.GetRepository<MediaFile>();
                repo.Add(file);
                unitOfWork.SaveChanges();
            }
        }

        public async Task UpdateDailyStatisticsAsync(int jobTriggerId, IReadOnlyCollection<MediaFileDto> files)
        {
            DateTime originalFrom = files.Min(x => x.Date).Date;
            DateTime from = originalFrom;
            var to = files.Max(x => x.Date).Date;

            Dictionary<DateTime, DailyStatistic> dailyStat;

            using (var unitOfWork = factory.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
            {
                IBaseRepository<DailyStatistic> repo = unitOfWork.GetRepository<DailyStatistic>();

                var daily = await repo.FindManyAsync(x => x.JobTriggerId == jobTriggerId && x.Period >= originalFrom && x.Period <= to);

                var newItems = new List<DailyStatistic>();
                do
                {
                    if (!daily.Exists(d => d.Period == from))
                    {
                        newItems.Add(new DailyStatistic
                        {
                            JobTriggerId = jobTriggerId,
                            Period = from,
                            FilesCount = 0,
                            FilesSize = 0,
                            TotalDuration = 0
                        });
                    }
                    from = from.AddDays(1);
                } while (from <= to);

                if (newItems.Count > 0)
                {
                    repo.AddRange(newItems);
                    unitOfWork.SaveChanges();
                }
            }

            using (var unitOfWork = factory.CreateUnitOfWork())
            {
                IBaseRepository<DailyStatistic> repo = unitOfWork.GetRepository<DailyStatistic>();
                var daily = await repo.FindManyAsync(x => x.JobTriggerId == jobTriggerId && x.Period >= originalFrom && x.Period <= to);
                dailyStat = daily.ToDictionary(k => k.Period, v => v);

                var group = files.GroupBy(x => x.Date.Date)
                    .Select(x => new
                    {
                        Date = x.Key,
                        Count = x.Count(),
                        Size = x.Sum(p => p.Size),
                        Duration = x.Sum(p => p.Duration)
                    });

                foreach (var item in group)
                {
                    var day = dailyStat[item.Date];
                    day.FilesCount += item.Count;
                    day.FilesSize += item.Size;
                    day.TotalDuration += item.Duration;
                    repo.Update(day);
                }
            }
        }

        public async Task DeleteObsoleteJobsAsync(int[] triggers, DateTime to)
        {
            using (var unitOfWork = factory.CreateUnitOfWork())
            {
                var triggerRepo = unitOfWork.GetRepository<JobTrigger>();
                var jobRepo = unitOfWork.GetRepository<HikJob>();
                var filesRepo = unitOfWork.GetRepository<MediaFile>();

                foreach (var trigger in triggers)
                {
                    logger.Information("Try cleanup {trigger}", trigger);
                    var jobTrigger = await triggerRepo.FindByAsync(x => x.Id == trigger);
                    if (jobTrigger != null)
                    {
                       logger.Information("{trigger} found, Id = {Id}, To = {to}", trigger, jobTrigger.Id, to);

                       var files = await filesRepo.FindManyAsync(x => x.JobTriggerId == jobTrigger.Id && x.Date <= to);
                        filesRepo.RemoveRange(files);
                        unitOfWork.SaveChanges();
                        logger.Information("{trigger} files cleared", trigger);

                        var jobs = await jobRepo.FindManyAsync(x => x.JobTriggerId == jobTrigger.Id && x.Finished <= to, x => x.DownloadedFiles);
                        jobRepo.RemoveRange(jobs);
                        unitOfWork.SaveChanges();
                        logger.Information("{trigger} jobs cleared", trigger);
                    }
                }

                unitOfWork.SaveChanges();
                logger.Information("Cleanup done");
            }
        }

        static void configureAutoMapper(IMapperConfigurationExpression x)
        {
            x.AddProfile<HikMappingProfile>();
        }
    }
}
