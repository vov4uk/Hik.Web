using Autofac;
using Hik.Client.Infrastructure;
using Hik.Client.Service;
using Hik.DataAccess;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Job.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        public override async Task<IReadOnlyCollection<MediaFileDTO>> RunAsync()
        {
            var downloader = AppBootstrapper.Container.Resolve<VideoDownloaderService>();
            downloader.ExceptionFired += base.ExceptionFired;
            downloader.FileDownloaded += Downloader_VideoDownloaded;
            LogInfo($"{Config} - {this.JobInstance.PeriodStart.Value} - {this.JobInstance.PeriodEnd.Value}");
            var files = await downloader.ExecuteAsync(Config, this.JobInstance.PeriodStart.Value, this.JobInstance.PeriodEnd.Value);
            downloader.ExceptionFired -= base.ExceptionFired;
            downloader.FileDownloaded -= Downloader_VideoDownloaded;
            return files;
        }

        public override Task SaveHistoryAsync(IReadOnlyCollection<MediaFile> files, JobService service)
        {
            return Task.CompletedTask;
        }

        public override Task SaveResultsAsync(IReadOnlyCollection<MediaFileDTO> files, JobService service)
        {
            return Task.CompletedTask;
        }

        private async void Downloader_VideoDownloaded(object sender, Hik.Client.Events.FileDownloadedEventArgs e)
        {
            var jobResultSaver = new JobService(this.unitOfWorkFactory, JobInstance);
            JobInstance.FilesCount++;
            var files = new[] { e.File };
            var mediaFiles = await jobResultSaver.SaveFilesAsync(files);
            await jobResultSaver.UpdateDailyStatisticsAsync(files);
            await jobResultSaver.SaveHistoryFilesAsync<DownloadHistory>(mediaFiles);
        }
    }
}