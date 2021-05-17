using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Hik.DTO.Contracts;
using Hik.Client.Service;
using Hik.Client.Infrastructure;
using Hik.DTO.Config;
using Hik.DataAccess;
using Hik.DataAccess.Data;
using Job.Extensions;

namespace Job.Impl
{
    public class HikVideoDownloaderJob : JobProcessBase
    {
        public HikVideoDownloaderJob(string trigger, string configFilePath, string connectionString, Guid activityId) 
            : base(trigger, configFilePath, connectionString, activityId)
        {
            Config = HikConfigExtensions.GetConfig<CameraConfig>(configFilePath);
            LogInfo(Config?.ToString());
        }

        public async override Task<IReadOnlyCollection<MediaFileDTO>> Run()
        { 
            var downloader = AppBootstrapper.Container.Resolve<VideoDownloaderService>();
            downloader.ExceptionFired += base.ExceptionFired;
            downloader.FileDownloaded += Downloader_VideoDownloaded;
            LogInfo($"{Config} - {this.JobInstance.PeriodStart.Value} - {this.JobInstance.PeriodEnd.Value}");
            return await downloader.ExecuteAsync(Config, this.JobInstance.PeriodStart.Value, this.JobInstance.PeriodEnd.Value);
        }

        public override Task SaveHistory(IReadOnlyCollection<MediaFile> files, JobService service)
        {
            return Task.CompletedTask;
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
            var files = new[] { e.File };
            var mediaFiles = await jobResultSaver.SaveFilesAsync(files);
            await jobResultSaver.UpdateDailyStatistics(files);
            await jobResultSaver.SaveHistoryFilesAsync<DownloadHistory>(mediaFiles);

            LogInfo("Save Video to DB. Done");
        }
    }
}
