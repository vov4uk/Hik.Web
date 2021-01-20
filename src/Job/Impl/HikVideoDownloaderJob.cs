using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Hik.DTO.Contracts;
using HikConsole.Scheduler;
using Hik.Client.Service;
using Hik.Client.Infrastructure;
using Hik.DTO.Config;

namespace Job.Impl
{
    public class HikVideoDownloaderJob : JobProcessBase
    {
        public HikVideoDownloaderJob(string description, string configFilePath, string connectionString, Guid activityId) 
            : base(description, configFilePath, connectionString, activityId)
        {
            Config = HikConfig.GetConfig<CameraConfig>(configFilePath);
        }

        public override JobType JobType => JobType.HikVideoDownloader;

        public override async Task InitializeProcessingPeriod()
        {
            var config = Config as CameraConfig;
            DateTime jobStart = DateTime.Now;

            var camera = await GetCamera(Config.Alias);
            DateTime?  lastSync = camera?.LastVideoSync;

            DateTime periodStart = lastSync?.AddMinutes(-1) ?? jobStart.AddHours(-1 * config.ProcessingPeriodHours);

            this.JobInstance.PeriodStart = periodStart;
            this.JobInstance.PeriodEnd = jobStart;
        }

        public async override Task<IReadOnlyCollection<MediaFileBase>> Run()
        { 
            var downloader = AppBootstrapper.Container.Resolve<HikVideoDownloaderService>();
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
            await jobResultSaver.SaveVideoAsync(e.File as FileDTO, Config);
            
            base.Logger.Info("Save Video to DB. Done");
        }
    }
}
