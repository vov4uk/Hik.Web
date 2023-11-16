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
using System.Diagnostics;
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

        protected override HikJob GetHikJob()
        {
            var job = base.GetHikJob();
            (DateTime PeriodStart, DateTime PeriodEnd) = HikConfigExtensions.CalculateProcessingPeriod(Config, jobTrigger.LastSync);
            logger.Information("Last sync - {LastSync}, Period - {PeriodStart} - {PeriodEnd}", jobTrigger.LastSync, PeriodStart, PeriodEnd);
            job.PeriodStart = PeriodStart;
            job.PeriodEnd = PeriodEnd;
            job.LatestFileEndDate = jobTrigger.LastSync;
            return job;
        }

        protected override async Task SaveResultsAsync(IReadOnlyCollection<MediaFileDto> files)
        {
            await db.UpdateDailyStatisticsAsync(jobTrigger.Id, files);
        }

        private void Downloader_VideoDownloaded(object sender, Hik.Client.Events.FileDownloadedEventArgs e)
        {
            try
            {
                JobInstance.FilesCount++;
                JobInstance.LatestFileEndDate = e.File.Date.AddSeconds(e.File.Duration ?? 0);
                db.UpdateJob(JobInstance);

                db.SaveFile(JobInstance, e.File);
            }
            catch (Exception ex)
            {
                logger.Error("Downloader_VideoDownloaded: {errorMsg}; Trace: {trace}", GetFullMessage(ex), ex.ToStringDemystified());
                HandleError(ex);
            }
        }
    }
}