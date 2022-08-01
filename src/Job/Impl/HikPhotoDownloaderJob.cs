using CSharpFunctionalExtensions;
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
    public class HikPhotoDownloaderJob : JobProcessBase<CameraConfig>
    {
        private readonly IHikPhotoDownloaderService service;
        public HikPhotoDownloaderJob(
            string trigger,
            CameraConfig config,
            IHikPhotoDownloaderService service,
            IHikDatabase db,
            IEmailHelper email,
            ILogger logger)
            : base(trigger, config, db, email, logger)
        {
            this.service = service;
        }

        protected override async Task<Result<IReadOnlyCollection<MediaFileDto>>> RunAsync()
        {
            var period = HikConfigExtensions.CalculateProcessingPeriod(Config, jobTrigger.LastSync);
            logger.LogInformation($"Last sync from DB - {jobTrigger.LastSync}, Period - {period.PeriodStart} - {period.PeriodEnd}");
            JobInstance.PeriodStart = period.PeriodStart;
            JobInstance.PeriodEnd = period.PeriodEnd;

            var files = await service.ExecuteAsync(Config, this.JobInstance.PeriodStart.Value, this.JobInstance.PeriodEnd.Value);
            return files;
        }
    }
}