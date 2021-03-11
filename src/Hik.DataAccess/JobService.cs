using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;

namespace Hik.DataAccess
{
    public class JobService
    {
        private readonly IUnitOfWorkFactory factory;
        private readonly HikJob job;
        private static readonly IMapper mapper = new MapperConfiguration(configureAutoMapper).CreateMapper();

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

            if (job.Success && job.PeriodEnd.HasValue)
            {
                var triggerRepo = unitOfWork.GetRepository<JobTrigger>();
                var trigger = await triggerRepo.FindByAsync(x => x.Id == job.JobTriggerId);
                trigger.LastSync = job.PeriodEnd;
            }

            await unitOfWork.SaveChangesAsync(job);
        }

        public async Task SaveFilesAsync(IReadOnlyCollection<MediaFileDTO> files)
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

            await AddEntities(files.Select(x => mapper.Map<MediaFile>(x)).ToList(), unitOfWork);
            await unitOfWork.SaveChangesAsync(job);
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
