using System;
using System.Threading.Tasks;
using Job;

namespace HikWeb
{
    class CronTrigger : Quartz.IJob
    {
        public async Task Execute(Quartz.IJobExecutionContext context)
        {
            string className = context.MergedJobDataMap["Job"].ToString();
            string configPath = context.MergedJobDataMap["ConfigPath"].ToString();
            var jobType = (JobType)Enum.Parse(typeof(JobType), context.MergedJobDataMap["JobType"].ToString());
            var parameters = new Parameters(className, jobType.ToString(), configPath);
            var activity = new Activity(parameters);
            await activity.Start();
        }
    }
}
