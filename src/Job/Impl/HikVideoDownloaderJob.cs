using Hik.Client.Abstraction;
using Hik.DataAccess.Abstractions;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Job.Email;
using Job.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Job.Impl
{
    public class HikVideoDownloaderJob : JobProcessBase<CameraConfig>
    {
        private readonly IHikVideoDownloaderService downloader;

        public HikVideoDownloaderJob(
            string trigger,
            CameraConfig config,
            IHikVideoDownloaderService service,
            IHikDatabase db,
            IEmailHelper email,
            ILogger logger)
            : base(trigger, config, db, email, logger)
        {
            this.downloader = service;
        }

        protected override async Task<IReadOnlyCollection<MediaFileDto>> RunAsync()
        {
            downloader.ExceptionFired += base.ExceptionFired;
            downloader.FileDownloaded += this.Downloader_VideoDownloaded;

            var period = HikConfigExtensions.CalculateProcessingPeriod(Config, jobTrigger.LastSync);
            LogInfo($"Last sync from DB - {jobTrigger.LastSync}, Period - {period.PeriodStart} - {period.PeriodEnd}");
            JobInstance.PeriodStart = period.PeriodStart;
            JobInstance.PeriodEnd = period.PeriodEnd;
            await db.UpdateJobAsync(JobInstance);

            var files = await downloader.ExecuteAsync(Config, this.JobInstance.PeriodStart.Value, this.JobInstance.PeriodEnd.Value);
            downloader.ExceptionFired -= base.ExceptionFired;
            downloader.FileDownloaded -= this.Downloader_VideoDownloaded;
            return files;
        }

        protected override Task SaveResultsAsync(IReadOnlyCollection<MediaFileDto> files)
        {
            return Task.CompletedTask;
        }

        private async void Downloader_VideoDownloaded(object sender, Hik.Client.Events.FileDownloadedEventArgs e)
        {
            JobInstance.FilesCount++;
            var files = new[] { e.File };
            var mediaFiles = await db.SaveFilesAsync(JobInstance, files);

            await db.UpdateDailyStatisticsAsync(jobTrigger.Id, files);
            await db.SaveDownloadHistoryFilesAsync(JobInstance, mediaFiles);
            await db.UpdateJobAsync(JobInstance);
        }
    }
}