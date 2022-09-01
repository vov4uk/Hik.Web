using CSharpFunctionalExtensions;
using Hik.Client.Abstraction.Services;
using Hik.DataAccess.Abstractions;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Job.Email;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Job.Impl
{
    public class DetectPeopleJob : JobProcessBase<DetectPeopleConfig>
    {
        private readonly IDetectPeopleService worker;

        public DetectPeopleJob(
            string trigger,
            DetectPeopleConfig config,
            IDetectPeopleService service,
            IHikDatabase db,
            IEmailHelper email,
            ILogger logger)
            : base(trigger, config, db, email, logger)
        {
            this.worker = service;
            this.configValidator = new DetectPeopleConfigValidator();
        }

        protected override Task<Result<IReadOnlyCollection<MediaFileDto>>> RunAsync()
        {
            return worker.ExecuteAsync(Config, DateTime.MinValue, DateTime.MaxValue);
        }

        protected override async Task SaveResultsAsync(IReadOnlyCollection<MediaFileDto> files)
        {
            JobInstance.PeriodStart = JobInstance.JobTrigger?.LastSync ?? DateTime.Now;
            JobInstance.PeriodEnd = DateTime.Now;
            JobInstance.FilesCount = files.Count;

            await db.UpdateDailyStatisticsAsync(jobTrigger.Id, files);
        }
    }
}
