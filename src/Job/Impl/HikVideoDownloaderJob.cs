using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Hik.DTO.Contracts;
using Hik.Client.Service;
using Hik.Client.Infrastructure;
using Hik.DTO.Config;
using Hik.DataAccess;

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

        public async override Task<IReadOnlyCollection<MediaFileDTO>> Run()
        { 
            var downloader = AppBootstrapper.Container.Resolve<HikVideoDownloaderService>();
            downloader.ExceptionFired += base.ExceptionFired;
            downloader.FileDownloaded += Downloader_VideoDownloaded;
            LogInfo($"{Config} - {this.JobInstance.PeriodStart.Value} - {this.JobInstance.PeriodEnd.Value}");
            return await downloader.ExecuteAsync(Config, this.JobInstance.PeriodStart.Value, this.JobInstance.PeriodEnd.Value);
        }

        public override Task SaveResults(IReadOnlyCollection<MediaFileDTO> files, JobService service)
        {
            return Task.CompletedTask;
        }

        private async void Downloader_VideoDownloaded(object sender, Hik.Client.Events.FileDownloadedEventArgs e)
        {
            LogInfo("Save Video to DB...");
            var jobResultSaver = new JobService(this.UnitOfWorkFactory, JobInstance);
            JobInstance.FilesCount++;
            await jobResultSaver.SaveFilesAsync(new[] { e.File }, Config);

            LogInfo("Save Video to DB. Done");
        }
    }
}
