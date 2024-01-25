using CSharpFunctionalExtensions;
using Hik.Client.Abstraction.Services;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Job.Email;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Serilog;
using System.Linq;

namespace Job.Impl
{
    public abstract class CollectorBaseClass<T> : JobProcessBase<T>
        where T : ImagesCollectorConfig
    {
        private const string DateTimeFormat = "yyyy'-'MM'-'dd' 'HH':'mm':'ss";
        private const string TimeFormat = "HH':'mm':'ss";
        private readonly IRecurrentJob worker;

        public CollectorBaseClass(JobTrigger trigger,
            IRecurrentJob worker,
            IHikDatabase db,
            IEmailHelper email,
            ILogger logger)
            : base(trigger, db, email, logger)
        {
            this.worker = worker;
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
                email.Send($"{jobTrigger.TriggerKey}: {files.Count} taken. From {JobInstance.PeriodStart?.ToString(DateTimeFormat)} to {JobInstance.PeriodEnd?.ToString(TimeFormat)}",
                    "EOM");
            }

            db.SaveFiles(JobInstance, files);
            await db.UpdateDailyStatisticsAsync(jobTrigger.Id, files);
        }
    }
}
