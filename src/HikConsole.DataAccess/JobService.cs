using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using HikConsole.DataAccess;
using HikConsole.DataAccess.Data;
using HikConsole.DTO;
using HikConsole.DTO.Contracts;

namespace HikConsole.Scheduler
{
    public class JobService
    {

        private readonly IUnitOfWorkFactory factory;
        private readonly HikJob job;
        private readonly IMapper mapper;

        public JobService(IUnitOfWorkFactory factory, HikJob job)
        {
            this.factory = factory;
            this.job = job;

            Action<IMapperConfigurationExpression> configureAutoMapper = x =>
            {
                x.AddProfile<AutoMapperProfile>();
            };

            var mapperConfig = new MapperConfiguration(configureAutoMapper);
            this.mapper = mapperConfig.CreateMapper();
        }

        public async Task SaveJobResultAsync(JobResult result)
        {
            if (result == null)
            {
                return;
            }

            using (var unitOfWork = factory.CreateUnitOfWork())
            {
                var jobRepo = unitOfWork.GetRepository<HikJob>();
                var cameraRepo = unitOfWork.GetRepository<Camera>();
                this.job.PeriodStart = result?.PeriodStart;
                this.job.PeriodEnd = result?.PeriodEnd;
                await jobRepo.UpdateAsync(this.job);

                foreach (var cameraResult in result?.CameraResults)
                {
                    var camera = await cameraRepo.FindByAsync(x => x.Alias == cameraResult.Key);
                    if (camera == null)
                    {
                        camera = this.mapper.Map<Camera>(cameraResult.Value.Config);
                        camera = (await cameraRepo.AddAsync(camera)).Entity;
                        await unitOfWork.SaveChangesAsync();
                    }

                    if (!cameraResult.Value.Failed)
                    {
                        camera.LastSync = result.PeriodEnd;
                    }

                    this.job.FailsCount += cameraResult.Value.Failed ? 1 : 0;

                    this.job.PhotosCount += cameraResult.Value.DownloadedPhotos.Count;
                    this.job.VideosCount += cameraResult.Value.DownloadedVideos.Count;
                    this.job.PhotosCount += cameraResult.Value.DeletedFiles.Count(x => x.Extention == ".jpg");
                    this.job.VideosCount += cameraResult.Value.DeletedFiles.Count(x => x.Extention == ".mp4");

                    // await this.AddEntities(cameraResult.Value.DownloadedVideos.Select(x => this.mapper.Map<Video>(x)).ToList(), unitOfWork);
                    await this.AddEntities(cameraResult.Value.DownloadedPhotos.Select(x => this.mapper.Map<Photo>(x)).ToList(), unitOfWork);
                    await this.AddEntities(cameraResult.Value.DeletedFiles.Select(x => this.mapper.Map<DeletedFile>(x)).ToList(), unitOfWork);

                    var status = cameraResult.Value.HardDriveStatus;
                    if (status != null)
                    {
                        await this.AddEntities(this.mapper.Map<HardDriveStatus>(status), unitOfWork);
                    }

                    await unitOfWork.SaveChangesAsync(this.job, camera);
                }
            }
        }

        public async Task SaveVideoAsync(VideoDTO videoDTO, string cameraAllias)
        {
            using (var unitOfWork = this.factory.CreateUnitOfWork())
            {
                var cameraRepo = unitOfWork.GetRepository<Camera>();
                var camera = await cameraRepo.FindByAsync(x => x.Alias == cameraAllias);

                var video = this.mapper.Map<Video>(videoDTO);

                await this.AddEntities(video, unitOfWork);

                await unitOfWork.SaveChangesAsync(job, camera);
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
