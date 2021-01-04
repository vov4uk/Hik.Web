using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;
using HikConsole.Scheduler;
using Hik.Client.Infrastructure;
using Hik.Client.Service;

namespace Job.Impl
{
    public class HikPhotoDownloaderJob : JobProcessBase
    {
        public HikPhotoDownloaderJob(string description, string path, string connectionString, Guid activityId) 
            : base(description, path, connectionString, activityId)
        {

        }

        public override JobType JobType => JobType.HikPhotoDownloader;

        public override async Task InitializeProcessingPeriod()
        {
            DateTime? lastSync;
            DateTime jobStart = JobInstance.Started;

            using (var unitOfWork = this.UnitOfWorkFactory.CreateUnitOfWork())
            {
                var cameraRepo = unitOfWork.GetRepository<Camera>();
                Camera camera = await cameraRepo.FindByAsync(x => x.Alias == AppConfig.Camera.Alias);
                lastSync = camera?.LastPhotoSync;
            }

            DateTime periodStart = lastSync?.AddMinutes(-1) ?? jobStart.AddHours(-1 * AppConfig.ProcessingPeriodHours);

            this.JobInstance.PeriodStart = periodStart;
            this.JobInstance.PeriodEnd = jobStart;
        }

        public async override Task<IReadOnlyCollection<MediaFileBase>> Run()
        {
            var downloader = AppBootstrapper.Container.Resolve<HikPhotoDownloaderService>();
            downloader.ExceptionFired += base.ExceptionFired;

            return await downloader.ExecuteAsync(AppConfig.Camera, this.JobInstance.PeriodStart.Value, this.JobInstance.PeriodEnd.Value);
        }

        public override async Task SaveResults(IReadOnlyCollection<MediaFileBase> files, JobService service)
        {
            JobInstance.PhotosCount += files.Count;

            var convertedFiles = files.OfType<PhotoDTO>().ToList();
            JobInstance.PhotosCount = convertedFiles.Count();
            await service.SavePhotosAsync(convertedFiles, AppConfig.Camera);
        }
    }
}
