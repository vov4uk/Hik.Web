using HikConsole.Scheduler;
using Quartz;
using System;
using System.Threading.Tasks;

namespace HikWeb.Job
{
    public class DeleteArchivingJob : BaseJob
    {
        public async override Task InternalExecute(IJobExecutionContext context)
        {
            var archivingJob = new DeleteArchiving(logger);
            var result = archivingJob.Archive(appConfig.Cameras, TimeSpan.FromDays(appConfig.RetentionPeriodDays.Value));
            var jobResultSaver = new JobResultsSaver(appConfig.ConnectionString, result, logger);
            await jobResultSaver.SaveAsync();
        }
    }
}
