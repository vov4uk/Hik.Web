using System.Threading.Tasks;
using HikConsole.Config;
using HikConsole.DataAccess;
using HikConsole.DataAccess.Data;

namespace HikConsole.Scheduler
{
    public class JobResultsSaver
    {
        private readonly string connectionString;
        private readonly JobResult result;

        public JobResultsSaver(string connectionString, JobResult result)
        {
            this.connectionString = connectionString;
            this.result = result;
        }

        public async Task SaveAsync()
        {
            using (var unitOfWork = new UnitOfWorkFactory().CreateUnitOfWork(this.connectionString))
            {
                var jobRepo = unitOfWork.GetRepository<Job>();
                var hdRepo = unitOfWork.GetRepository<HardDriveStatus>();
                var videoRepo = unitOfWork.GetRepository<Video>();
                var photoRepo = unitOfWork.GetRepository<Photo>();
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

                    await hdRepo.Add(cameraResult.Value.HardDriveStatus);
                    await videoRepo.AddRange(cameraResult.Value.DownloadedVideos);
                    await photoRepo.AddRange(cameraResult.Value.DownloadedPhotos);

                    await unitOfWork.SaveChangesAsync(this.result.Job, camera);
                }
            }
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
    }
}
