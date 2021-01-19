using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Hik.Client.Infrastructure;
using Hik.Client.Service;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using HikConsole.Scheduler;

namespace Job.Impl
{
    public class YiVideoDownloaderJob : JobProcessBase
    {
        public YiVideoDownloaderJob(string trigger, string configFilePath, string connectionString, Guid activityId) 
            : base(trigger, configFilePath, connectionString, activityId)
        {
            Config = HikConfig.GetConfig<CameraConfig>(configFilePath);
        }

        public override JobType JobType => JobType.YiVideoDownloader;

        public override async Task InitializeProcessingPeriod()
        {
            var config = Config as CameraConfig;
            DateTime? lastSync = null;
            DateTime jobStart = DateTime.Now;

            using (var unitOfWork = this.UnitOfWorkFactory.CreateUnitOfWork())
            {
                var cameraRepo = unitOfWork.GetRepository<Camera>();
                Camera camera = await cameraRepo.FindByAsync(x => x.Alias == Config.Alias);
                lastSync = camera?.LastVideoSync;
            }

            DateTime periodStart = lastSync?.AddMinutes(-1) ?? jobStart.AddHours(-1 * config.ProcessingPeriodHours);

            this.JobInstance.PeriodStart = periodStart;
            this.JobInstance.PeriodEnd = jobStart;
        }

        public async override Task<IReadOnlyCollection<MediaFileBase>> Run()
        {
            var downloader = AppBootstrapper.Container.Resolve<YiVideoDownloaderService>();
            downloader.ExceptionFired += base.ExceptionFired;
            downloader.FileDownloaded += Downloader_VideoDownloaded;

            return await downloader.ExecuteAsync(Config, this.JobInstance.PeriodStart.Value, this.JobInstance.PeriodEnd.Value);
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
            await jobResultSaver.SaveVideoAsync(e.File as VideoDTO, Config);

            base.Logger.Info("Save Video to DB. Done");
        }
    }
}
