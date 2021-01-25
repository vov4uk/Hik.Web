using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;

namespace Hik.DataAccess
{
    public class JobService
    {
        private readonly IUnitOfWorkFactory factory;
        private readonly HikJob job;
        private static readonly IMapper mapper;

        public JobService(IUnitOfWorkFactory factory, HikJob job)
        {
            this.factory = factory;
            this.job = job;
        }

        static JobService()
        {
            static void configureAutoMapper(IMapperConfigurationExpression x)
            {
                x.AddProfile<AutoMapperProfile>();
            }

            var mapperConfig = new MapperConfiguration(configureAutoMapper);
            mapper = mapperConfig.CreateMapper();
        }

        public async Task SaveJobResultAsync(BaseConfig config)
        {
            using var unitOfWork = factory.CreateUnitOfWork();
            var jobRepo = unitOfWork.GetRepository<HikJob>();
            job.Finished = DateTime.Now;            

            Camera camera = await GetCameraSafe(config, unitOfWork);

            if (job.Success)
            {
                camera.LastSync = job.PeriodEnd;
            }
            await jobRepo.UpdateAsync(job);
            await unitOfWork.SaveChangesAsync(job, camera);
        }

        public async Task SaveFilesAsync(IReadOnlyCollection<MediaFileDTO> files, BaseConfig cameraConfig)
        {
            using var unitOfWork = factory.CreateUnitOfWork();
            Camera camera = await GetCameraSafe(cameraConfig, unitOfWork);

            var from = files.Min(x => x.Date).Date;
            var to = files.Max(x => x.Date).Date;

            Dictionary<DateTime, DailyStatistic> dailyStat = (await GetDailyStatisticSafe(camera.Id, from, to, unitOfWork)).ToDictionary(k => k.Period, v => v);

            var group = files.GroupBy(x => x.Date.Date)
                .Select(x => new { date = x.Key, cnt = x.Count(), size = x.Sum(p => p.Size) });

            foreach (var item in group)
            {
                var day = dailyStat[item.date];
                day.FilesCount += item.cnt;
                day.FilesSize += item.size;
            }

            await AddEntities(files.Select(x => mapper.Map<MediaFile>(x)).ToList(), unitOfWork);
            await SaveDailyStatisticSafe(camera.Id, dailyStat.Values, unitOfWork);
            await unitOfWork.SaveChangesAsync(job, camera);
        }

        private async Task<Camera> GetCameraSafe(BaseConfig config, IUnitOfWork unitOfWork)
        {
            var cameraRepo = unitOfWork.GetRepository<Camera>();
            var camera = await cameraRepo.FindByAsync(x => x.Alias == config.Alias);
            if (camera == null)
            {
                camera = mapper.Map<Camera>(config);
                camera = (await cameraRepo.AddAsync(camera)).Entity;
                await unitOfWork.SaveChangesAsync();
            }

            return camera;
        }

        private async Task<List<DailyStatistic>> GetDailyStatisticSafe(int cameraId, DateTime periodStart, DateTime periodEnd, IUnitOfWork unitOfWork)
        {
            var repo = unitOfWork.GetRepository<DailyStatistic>();
            var daily = await repo.FindManyAsync(x => x.CameraId == cameraId);

            var from = periodStart.Date;
            var to = periodEnd.Date;

            do
            {
                if (!daily.Any(d => d.Period == from))
                {
                    var day = new DailyStatistic
                        {
                            CameraId = cameraId,
                            Period = from,
                            FilesCount = 0,
                            FilesSize = 0
                        };
                    daily.Add(day);
                }
                from = from.AddDays(1);
            } while (from <= to);

            return daily;
        }

        private async Task SaveDailyStatisticSafe(int cameraId, ICollection<DailyStatistic> ds, IUnitOfWork unitOfWork)
        {
            var repo = unitOfWork.GetRepository<DailyStatistic>();
            var daily = await repo.FindManyAsync(x => x.CameraId == cameraId);

            var itemsToAdd = new List<DailyStatistic>();
            foreach (var day in ds)
            {
                if (!daily.Any(d => d.Period == day.Period) && day.FilesCount > 0 && day.FilesSize > 0)
                {
                    itemsToAdd.Add(day);
                }
            }

            if (itemsToAdd.Any())
            {
                await repo.AddRangeAsync(itemsToAdd);
            }
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
    }
}
