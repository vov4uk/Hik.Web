using HikConsole.DTO;
using HikConsole.Scheduler;
using Quartz;
using System;
using System.Threading.Tasks;

namespace HikWeb.Job
{
    public class DeleteArchivingJob : BaseJob
    {
        public override async Task<JobResult> InternalExecute(IJobExecutionContext context)
        {
            var archivingJob = new DeleteArchiving(Logger);
            var result = await archivingJob.Archive(Config.Cameras, TimeSpan.FromDays(Config.RetentionPeriodDays.Value), Config.FilesToDelete);
            return result;
        }
    }
}
