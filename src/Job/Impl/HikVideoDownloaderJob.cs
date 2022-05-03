using Autofac;
using Hik.Client.Abstraction;
using Hik.Client.Infrastructure;
using Hik.DataAccess.Abstractions;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Job.Email;
using Job.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Job.Impl
{
    public class HikVideoDownloaderJob : JobProcessBase
    {
        public HikVideoDownloaderJob(string trigger, string configFilePath, IHikDatabase db, IEmailHelper email, Guid activityId)
            : base(trigger, db, email, activityId)
        {
            Config = HikConfigExtensions.GetConfig<CameraConfig>(configFilePath);
            LogInfo(Config?.ToString());
        }

        protected override async Task<IReadOnlyCollection<MediaFileDTO>> RunAsync()
        {
            var downloader = AppBootstrapper.Container.Resolve<IHikVideoDownloaderService>();
            downloader.ExceptionFired += base.ExceptionFired;
            downloader.FileDownloaded += this.Downloader_VideoDownloaded;

            var period = HikConfigExtensions.CalculateProcessingPeriod(Config, jobTrigger.LastSync);
            LogInfo($"Last sync from DB - {jobTrigger.LastSync}, Period - {period.PeriodStart} - {period.PeriodEnd}");
            JobInstance.PeriodStart = period.PeriodStart;
            JobInstance.PeriodEnd = period.PeriodEnd;

            var files = await downloader.ExecuteAsync(Config, this.JobInstance.PeriodStart.Value, this.JobInstance.PeriodEnd.Value);
            downloader.ExceptionFired -= base.ExceptionFired;
            downloader.FileDownloaded -= this.Downloader_VideoDownloaded;
            return files;
        }

        protected override Task SaveResultsAsync(IReadOnlyCollection<MediaFileDTO> files)
        {
            return Task.CompletedTask;
        }

        private async void Downloader_VideoDownloaded(object sender, Hik.Client.Events.FileDownloadedEventArgs e)
        {
            JobInstance.FilesCount++;
            var files = new[] { e.File };
            var mediaFiles = await db.SaveFilesAsync(JobInstance, files);
            await db.UpdateDailyStatisticsAsync(JobInstance, files);
            await db.SaveDownloadHistoryFilesAsync(JobInstance, mediaFiles);
        }
    }
}