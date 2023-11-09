using CSharpFunctionalExtensions;
using Hik.Client.Abstraction.Services;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Job.Email;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Job.Impl
{
    public class ArchiveJob : JobProcessBase<ArchiveConfig>
    {
        private const string DateTimeFormat = "yyyy'-'MM'-'dd' 'HH':'mm':'ss";
        private const string TimeFormat = "HH':'mm':'ss";
        private readonly IArchiveService worker;

        public ArchiveJob(JobTrigger trigger,
            IArchiveService worker,
            IHikDatabase db,
            IEmailHelper email,
            ILogger logger)
            : base(trigger, db, email, logger)
        {
            this.worker = worker;
            this.configValidator = new ArchiveConfigValidator();
        }

        protected override Task<Result<IReadOnlyCollection<MediaFileDto>>> RunAsync()
        {
            return worker.ExecuteAsync(Config, DateTime.MinValue, DateTime.MaxValue);
        }

        protected override async Task SaveResultsAsync(IReadOnlyCollection<MediaFileDto> files)
        {
            JobInstance.PeriodStart = files.Min(x => x.Date);
            JobInstance.PeriodEnd = files.Max(x => x.Date);
            JobInstance.FilesCount = files.Count;

            var abnormalFilesCount = Config.AbnormalFilesCount;
            if (abnormalFilesCount > 0 && files.Count > abnormalFilesCount)
            {
                email.Send($"{jobTrigger.ClassName}: {files.Count} taken. From {JobInstance.PeriodStart?.ToString(DateTimeFormat)} to {JobInstance.PeriodEnd?.ToString(TimeFormat)}",
                    "EOM");
            }

            await db.UpdateDailyStatisticsAsync(jobTrigger.Id, files);

            var mediaFiles = await db.SaveFilesAsync(JobInstance, files);
            await db.SaveDownloadHistoryFilesAsync(JobInstance, mediaFiles);
        }
    }
}