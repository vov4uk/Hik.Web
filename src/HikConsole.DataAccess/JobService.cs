using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HikConsole.DataAccess;
using HikConsole.DataAccess.Data;
using HikConsole.DTO;
using HikConsole.DTO.Contracts;

namespace HikConsole.Scheduler
{
    public class JobService
    {

        private readonly IUnitOfWorkFactory factory;
        private readonly JobResult result;
        private readonly HikJob job;

        public JobService(IUnitOfWorkFactory factory, HikJob job, JobResult result)
        {
            this.factory = factory;
            this.job = job;
            this.result = result;
        }

        public async Task SaveAsync()
        {

            using (var unitOfWork = factory.CreateUnitOfWork())
            {
                var jobRepo = unitOfWork.GetRepository<HikJob>();
                var cameraRepo = unitOfWork.GetRepository<Camera>();
                this.job.PeriodStart = this.result?.PeriodStart;
                this.job.PeriodEnd = this.result?.PeriodEnd;
                await jobRepo.Update(this.job);

                foreach (var cameraResult in this.result?.CameraResults)
                {
                    var camera = await cameraRepo.FindBy(x => x.Alias == cameraResult.Key);
                    if (camera == null)
                    {
                        camera = this.GetCamera(cameraResult.Value.Config);
                        await cameraRepo.Add(camera);
                    }

                    if (!cameraResult.Value.Failed)
                    {
                        camera.LastSync = this.result.PeriodEnd; 
                    }

                    this.job.FailsCount += cameraResult.Value.Failed ? 1 : 0;

                    this.job.PhotosCount += cameraResult.Value.DownloadedPhotos.Count;
                    this.job.VideosCount += cameraResult.Value.DownloadedVideos.Count;
                    this.job.PhotosCount += cameraResult.Value.DeletedFiles.Count(x => x.Extention == ".jpg");
                    this.job.VideosCount += cameraResult.Value.DeletedFiles.Count(x => x.Extention == ".mp4");

                    await this.AddEntities(cameraResult.Value.DownloadedVideos.Select(x => new Video { DownloadStartTime = x.DownloadStartTime, DownloadStopTime = x.DownloadStopTime, Name = x.Name, Size = x.Size, StartTime = x.StartTime, StopTime = x.StopTime }).ToList(), unitOfWork);
                    await this.AddEntities(cameraResult.Value.DownloadedPhotos.Select(x => new Photo { Name = x.Name, Size = x.Size, DateTaken = x.DateTaken, DownloadStartTime = x.DownloadStartTime, DownloadStopTime = x.DownloadStopTime }).ToList(), unitOfWork);
                    await this.AddEntities(cameraResult.Value.DeletedFiles.Select(x => new DeletedFile { FilePath = x.FilePath, Extention = x.Extention }).ToList(), unitOfWork);

                    var status = cameraResult.Value.HardDriveStatus;
                    if (status != null)
                    {
                        await this.AddEntities(new HardDriveStatus { Capacity = status.Capacity, FreePictureSpace = status.FreePictureSpace, FreeSpace = status.FreeSpace, HDAttr = status.HDAttr, HdStatus = status.HdStatus, HDType = status.HDType, PictureCapacity = status.PictureCapacity, Recycling = status.Recycling }, unitOfWork);
                    }

                    await unitOfWork.SaveChangesAsync(this.job, camera);
                }
            }
        }

        public Camera GetCamera(CameraDTO cameraConf)
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

        private Task AddEntities<TEntity>(List<TEntity> entities, IUnitOfWork unitOfWork)
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
