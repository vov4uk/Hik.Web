using CSharpFunctionalExtensions;
using Hik.Client.Abstraction.Services;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Job.Email;
using Job.Extensions;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Job.Impl
{
    public class HikPhotoDownloaderJob : JobProcessBase<CameraConfig>
    {
        private readonly IHikPhotoDownloaderService service;
        public HikPhotoDownloaderJob(
            JobTrigger trigger,
            IHikPhotoDownloaderService service,
            IHikDatabase db,
            IEmailHelper email,
            ILogger logger)
            : base(trigger, db, email, logger)
        {
            this.service = service;
            this.configValidator = new CameraConfigValidator();
        }

        protected override async Task<Result<IReadOnlyCollection<MediaFileDto>>> RunAsync()
        {
            var files = await service.ExecuteAsync(Config, this.JobInstance.PeriodStart.Value, this.JobInstance.PeriodEnd.Value);
            return files;
        }

        protected override HikJob GetHikJob()
        {
            var job = base.GetHikJob();
            var period = HikConfigExtensions.CalculateProcessingPeriod(Config, jobTrigger.LastSync);
            logger.Information("Last sync - {LastSync}, Period - {PeriodStart} - {PeriodEnd}", jobTrigger.LastSync, period.PeriodStart, period.PeriodEnd);
            job.PeriodStart = period.PeriodStart;
            job.PeriodEnd = period.PeriodEnd;
            return job;
        }
    }
}