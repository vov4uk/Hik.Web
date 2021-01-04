using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;
using HikConsole.Scheduler;
using Hik.Client.Service;
using Hik.Client.Infrastructure;

namespace Job.Impl
{
    public class HikVideoDownloaderJob : JobProcessBase
    {
        public HikVideoDownloaderJob(string description, string path, string connectionString, Guid activityId) 
            : base(description, path, connectionString, activityId)
        {

        }

        public override JobType JobType => JobType.HikVideoDownloader;

        public override async Task InitializeProcessingPeriod()
        {
            DateTime? lastSync = null;
            DateTime jobStart = DateTime.Now;

            using (var unitOfWork = this.UnitOfWorkFactory.CreateUnitOfWork())
            {
                var cameraRepo = unitOfWork.GetRepository<Camera>();
                Camera camera = await cameraRepo.FindByAsync(x => x.Alias == AppConfig.Camera.Alias);
                lastSync = camera?.LastVideoSync;
            }

            DateTime periodStart = lastSync?.AddMinutes(-1) ?? jobStart.AddHours(-1 * AppConfig.ProcessingPeriodHours);

            this.JobInstance.PeriodStart = periodStart;
            this.JobInstance.PeriodEnd = jobStart;
        }

        public async override Task<IReadOnlyCollection<MediaFileBase>> Run()
        { 
            var downloader = AppBootstrapper.Container.Resolve<HikVideoDownloaderService>();
            downloader.ExceptionFired += base.ExceptionFired;
            downloader.FileDownloaded += Downloader_VideoDownloaded;

            return await downloader.ExecuteAsync(AppConfig.Camera, this.JobInstance.PeriodStart.Value, this.JobInstance.PeriodEnd.Value);
        }

        public override Task SaveResults(IReadOnlyCollection<MediaFileBase> files, JobService service)
        {
            return Task.CompletedTask;
        }

        private async void Downloader_VideoDownloaded(object sender, Hik.Client.Events.FileDownloadedEventArgs e)
        {
            base.Logger.Info("Save Video to DB...");
            var jobResultSaver = new JobService(this.UnitOfWorkFactory, JobInstance);
            JobInstance.VideosCount++;
            await jobResultSaver.SaveVideoAsync(e.File as VideoDTO, AppConfig.Camera);
            
            base.Logger.Info("Save Video to DB. Done");
        }
    }
}
