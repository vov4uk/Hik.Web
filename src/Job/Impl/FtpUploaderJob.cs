using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Hik.Client.Abstraction.Services;
using Hik.DataAccess.Abstractions;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Job.Email;
using Microsoft.Extensions.Logging;

namespace Job.Impl
{
    public class FtpUploaderJob : JobProcessBase<FtpUploaderConfig>
    {
        private readonly IFtpUploaderService service;
        public FtpUploaderJob(string trigger,
            FtpUploaderConfig config,
            IFtpUploaderService service,
            IHikDatabase db,
            IEmailHelper email,
            ILogger logger)
            : base(trigger, config, db, email, logger)
        {
            this.service = service;
        }

        protected override Task<Result<IReadOnlyCollection<MediaFileDto>>> RunAsync()
        {
            return service.ExecuteAsync(Config, DateTime.MinValue, DateTime.MaxValue);
        }

        protected override async Task SaveResultsAsync(IReadOnlyCollection<MediaFileDto> files)
        {
            JobInstance.PeriodStart = JobInstance.JobTrigger?.LastSync ?? DateTime.Now;
            JobInstance.PeriodEnd = DateTime.Now;
            JobInstance.FilesCount = files.Count;

            var mediaFiles = await db.SaveFilesAsync(JobInstance, files);
            await db.SaveDownloadHistoryFilesAsync(JobInstance, mediaFiles);

            await db.UpdateDailyStatisticsAsync(jobTrigger.Id, files);
        }
    }
}
