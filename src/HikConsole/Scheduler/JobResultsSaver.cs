using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HikConsole.Abstraction;
using HikConsole.Config;
using HikConsole.DataAccess;
using HikConsole.DataAccess.Data;

namespace HikConsole.Scheduler
{
    public class JobResultsSaver
    {
        private readonly ILogger logger;
        private readonly string connectionString;
        private readonly JobResult result;

        public JobResultsSaver(string connectionString, JobResult result, ILogger logger)
        {
            this.connectionString = connectionString;
            this.result = result;
            this.logger = logger;
        }

        public async Task SaveAsync()
        {
            this.logger.Info("Saving results to DB...");

            try
            {
                using (var unitOfWork = new UnitOfWorkFactory().CreateUnitOfWork(this.connectionString))
                {
                    var jobRepo = unitOfWork.GetRepository<HikJob>();
                    var cameraRepo = unitOfWork.GetRepository<Camera>();
                    await jobRepo.Add(this.result.Job);

                    foreach (var cameraResult in this.result.CameraResults)
                    {
                        var camera = await cameraRepo.FindBy(x => x.Alias == cameraResult.Key);
                        if (camera == null)
                        {
                            camera = this.GetCamera(cameraResult.Value.Config);
                            await cameraRepo.Add(camera);
                        }

                        this.result.Job.FailsCount += cameraResult.Value.Failed ? 1 : 0;
                        this.result.Job.PhotosCount += cameraResult.Value.DownloadedPhotos.Count;
                        this.result.Job.VideosCount += cameraResult.Value.DownloadedVideos.Count;
                        this.result.Job.PhotosCount += cameraResult.Value.DeletedFiles.Count(x => x.Extention == ".jpg");
                        this.result.Job.VideosCount += cameraResult.Value.DeletedFiles.Count(x => x.Extention == ".mp4");

                        await this.AddEntities<Video>(cameraResult.Value.DownloadedVideos, unitOfWork);
                        await this.AddEntities<Photo>(cameraResult.Value.DownloadedPhotos, unitOfWork);
                        await this.AddEntities<DeletedFile>(cameraResult.Value.DeletedFiles, unitOfWork);
                        await this.AddEntities(cameraResult.Value.HardDriveStatus, unitOfWork);
                        await unitOfWork.SaveChangesAsync(this.result.Job, camera);
                    }
                }
            }
            catch (Exception e)
            {
                this.logger.Error("Exception ocurred during saving to DB", e);
            }

            this.logger.Info("Saving results to DB. Done!");
        }

        public Camera GetCamera(CameraConfig cameraConf)
        {
            var cam = new Camera
            {
                Alias = cameraConf.Alias,
                DestinationFolder = cameraConf.DestinationFolder,
                IpAddress = cameraConf.IpAddress,
                PortNumber = cameraConf.PortNumber,
                UserName = cameraConf.UserName,
            };
            return cam;
        }

        private Task AddEntities<TEntity>(IEnumerable<TEntity> entities, IUnitOfWork unitOfWork)
            where TEntity : class
        {
            if (entities != null && entities.Any())
            {
                var repo = unitOfWork.GetRepository<TEntity>();
                return repo.AddRange(entities);
            }

            return Task.CompletedTask;
        }

        private Task AddEntities<TEntity>(TEntity entity, IUnitOfWork unitOfWork)
            where TEntity : class
        {
            if (entity != null)
            {
                var repo = unitOfWork.GetRepository<TEntity>();
                return repo.Add(entity).AsTask();
            }

            return Task.CompletedTask;
        }
    }
}
