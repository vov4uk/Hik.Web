using HikConsole.Scheduler;
using Quartz;
using System;
using System.Threading.Tasks;

namespace HikWeb.Job
{
    public class DeleteArchivingJob : BaseJob
    {
        public override async Task InternalExecute(IJobExecutionContext context)
        {
            var archivingJob = new DeleteArchiving(Logger);
            var result = await archivingJob.Archive(Config.Cameras, TimeSpan.FromDays(Config.RetentionPeriodDays.Value));
            var jobResultSaver = new JobResultsSaver(Config.ConnectionString, result, Logger);
            await jobResultSaver.SaveAsync();
        }
    }
}
