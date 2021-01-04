using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hik.DataAccess;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;

namespace HikConsole.Scheduler
{
    public class JobService
    {
        private readonly IUnitOfWorkFactory factory;
        private readonly HikJob job;
        private static IMapper mapper;

        public JobService(IUnitOfWorkFactory factory, HikJob job)
        {
            this.factory = factory;
            this.job = job;
        }

        static JobService()
        {
            Action<IMapperConfigurationExpression> configureAutoMapper = x =>
            {
                x.AddProfile<AutoMapperProfile>();
            };

            var mapperConfig = new MapperConfiguration(configureAutoMapper);
            mapper = mapperConfig.CreateMapper();
        }

        public async Task SaveJobResultAsync(CameraConfig cameraConfig) 
        {
            using (var unitOfWork = factory.CreateUnitOfWork())
            {
                var jobRepo = unitOfWork.GetRepository<HikJob>();
                this.job.Finished = DateTime.Now;
                await jobRepo.UpdateAsync(this.job);

                Camera camera = await GetCameraSafe(cameraConfig, unitOfWork);

                if (job.Success)
                {
                    if(job.JobType.Contains("Photo"))
                    {
                        camera.LastPhotoSync = job.PeriodEnd;
                    }
                    else if (job.JobType.Contains("Video"))
                    {
                        camera.LastVideoSync = job.PeriodEnd;
                    }
                }

                await unitOfWork.SaveChangesAsync(this.job, camera);
            }
        }

        public async Task SaveVideoAsync(VideoDTO videoDTO, CameraConfig cameraConfig)
        {
            using (var unitOfWork = this.factory.CreateUnitOfWork())
            {
                var camera = await this.GetCameraSafe(cameraConfig, unitOfWork);

                var video = mapper.Map<Video>(videoDTO);
                await this.AddEntities(video, unitOfWork);

                var dailyStat = await GetDailyStatisticSafe(camera.Id, videoDTO.StartTime, videoDTO.StopTime, unitOfWork);
                var stat = dailyStat.FirstOrDefault(x => x.Period == videoDTO.StartTime.Date);

                stat.VideosCount++;
                stat.VideosSize += videoDTO.Size;

                await unitOfWork.SaveChangesAsync(job, camera);
            }
        }

        public async Task SavePhotosAsync(IReadOnlyCollection<PhotoDTO> files, CameraConfig cameraConfig)
        {
            using (var unitOfWork = factory.CreateUnitOfWork())
            {
                Camera camera = await GetCameraSafe(cameraConfig, unitOfWork);

                var from = files.Min(x => x.DateTaken).Date;
                var to = files.Max(x => x.DateTaken).Date;

                var dailyStat = (await GetDailyStatisticSafe(camera.Id, from, to, unitOfWork)).ToDictionary(k => k.Period, v => v);

                var group = files.GroupBy(x => x.DateTaken.Date)
                    .Select(x => new { date = x.Key, cnt = x.Count(), size = x.Sum(p => p.Size) } );

                foreach (var item in group)
                {
                    var day = dailyStat[item.date];
                    day.PhotosCount += item.cnt ;
                    day.PhotosSize += item.size;
                }

                await this.AddEntities(files.Select(x => mapper.Map<Photo>(x)).ToList(), unitOfWork);
                await unitOfWork.SaveChangesAsync(this.job, camera);
            }
        }
        
        public async Task SaveDeletedFilesAsync(IReadOnlyCollection<DeletedFileDTO> files, CameraConfig cameraConfig)
        {
            using (var unitOfWork = factory.CreateUnitOfWork())
            {
                Camera camera = await GetCameraSafe(cameraConfig, unitOfWork);

                await this.AddEntities(files.Select(x => mapper.Map<DeletedFile>(x)).ToList(), unitOfWork);
                await unitOfWork.SaveChangesAsync(this.job, camera);
            }
        }

        private async Task<Camera> GetCameraSafe(CameraConfig cameraConfig, IUnitOfWork unitOfWork)
        {
            var cameraRepo = unitOfWork.GetRepository<Camera>();
            var camera = await cameraRepo.FindByAsync(x => x.Alias == cameraConfig.Alias);
            if (camera == null)
            {
                camera = mapper.Map<Camera>(cameraConfig);
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
                if(!daily.Any(d => d.Period == from))
                {
                    var day = (await repo.AddAsync(
                        new DailyStatistic { 
                            CameraId = cameraId, 
                            Period = from, 
                            PhotosCount = 0, 
                            PhotosSize = 0, 
                            VideosCount = 0, 
                            VideosSize = 0 })).Entity;
                    daily.Add(day);
                }
                from = from.AddDays(1);
            } while (from <= to);

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

        private Task AddEntities<TEntity>(TEntity entity, IUnitOfWork unitOfWork)
            where TEntity : class
        {
            if (entity != null)
            {
                var repo = unitOfWork.GetRepository<TEntity>();
                return repo.AddAsync(entity).AsTask();
            }

            return Task.CompletedTask;
        }
    }
}
