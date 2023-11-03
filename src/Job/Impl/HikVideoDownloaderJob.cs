using CSharpFunctionalExtensions;
using Hik.Client.Abstraction.Services;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Job.Email;
using Job.Extensions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Job.Impl
{
    public class HikVideoDownloaderJob : JobProcessBase<CameraConfig>
    {
        private readonly IHikVideoDownloaderService downloader;

        public HikVideoDownloaderJob(
            JobTrigger trigger,
            IHikVideoDownloaderService service,
            IHikDatabase db,
            IEmailHelper email,
            ILogger logger)
            : base(trigger, db, email, logger)
        {
            this.downloader = service;
            this.configValidator = new CameraConfigValidator();
        }

        protected override async Task<Result<IReadOnlyCollection<MediaFileDto>>> RunAsync()
        {
            downloader.FileDownloaded += this.Downloader_VideoDownloaded;
            var files = await downloader.ExecuteAsync(Config, JobInstance.PeriodStart.Value, JobInstance.PeriodEnd.Value);

            downloader.FileDownloaded -= this.Downloader_VideoDownloaded;
            return files;
        }

        protected override Task SaveResultsAsync(IReadOnlyCollection<MediaFileDto> files)
        {
            return Task.CompletedTask;
        }

        protected override void SetProcessingPeriod(HikJob job)
        {
            (DateTime PeriodStart, DateTime PeriodEnd) period = HikConfigExtensions.CalculateProcessingPeriod(Config, jobTrigger.LastSync);
            logger.Information("Last sync - {LastSync}, Period - {PeriodStart} - {PeriodEnd}", jobTrigger.LastSync, period.PeriodStart, period.PeriodEnd);
            JobInstance.PeriodStart = period.PeriodStart;
            JobInstance.PeriodEnd = period.PeriodEnd;
            JobInstance.LatestFileEndDate = jobTrigger.LastSync;
        }

        private async void Downloader_VideoDownloaded(object sender, Hik.Client.Events.FileDownloadedEventArgs e)
        {
            JobInstance.FilesCount++;
            JobInstance.LatestFileEndDate = e.File.Date.AddSeconds(e.File.Duration ?? 0);
            var files = new[] { e.File };
            var mediaFiles = await db.SaveFilesAsync(JobInstance, files);

            logger.Information("Downloader_VideoDownloaded : Update Daily Statistics");
            await db.UpdateDailyStatisticsAsync(JobInstance.JobTriggerId, files);
            logger.Information("Downloader_VideoDownloaded : Update Daily Statistics. Done");
            await db.SaveDownloadHistoryFilesAsync(JobInstance, mediaFiles);
        }
    }
}