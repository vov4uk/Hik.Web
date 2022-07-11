using Hik.Client.Abstraction;
using Hik.DataAccess.Abstractions;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Job.Email;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
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
        }

        protected override async Task<IReadOnlyCollection<MediaFileDto>> RunAsync()
        {
            worker.ExceptionFired += base.ExceptionFired;
            return await worker.ExecuteAsync(Config, DateTime.MinValue, DateTime.MaxValue);
        }

        protected override async Task SaveResultsAsync(IReadOnlyCollection<MediaFileDto> files)
        {
            JobInstance.PeriodStart = files.Min(x => x.Date);
            JobInstance.PeriodEnd = files.Max(x => x.Date);
            JobInstance.FilesCount = files.Count;

            await db.UpdateDailyStatisticsAsync(jobTrigger.Id, files);
        }
    }
}
