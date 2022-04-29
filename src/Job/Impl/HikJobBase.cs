using Hik.DataAccess.Abstractions;
using Hik.DTO.Config;
using Job.Email;
using System;

namespace Job.Impl
{
    public abstract class HikJobBase : JobProcessBase
    {
        protected HikJobBase(string trigger, IJobService db, IEmailHelper email, Guid activityId)
            : base(trigger, db, email, activityId)
        {
        }

        protected void CalculateProcessingPeriod()
        {
            var config = Config as CameraConfig;
            DateTime jobStart = DateTime.Now;

            DateTime? lastSync = jobTrigger.LastSync;
            DateTime periodStart = lastSync?.AddMinutes(-1) ?? jobStart.AddHours(-1 * config?.ProcessingPeriodHours ?? 1);

            this.JobInstance.PeriodStart = periodStart;
            this.JobInstance.PeriodEnd = jobStart;

            LogInfo($"Last sync from DB - {lastSync}, Period - {periodStart} - {jobStart}");
        }
    }
}
