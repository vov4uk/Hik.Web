using Quartz;
using System.Threading.Tasks;
using Job;

namespace Hik.Web.Scheduler
{
    class CronTrigger : IJob
    {
        // TODO - sent activity command (without connection string)
        public async Task Execute(IJobExecutionContext context)
        {
            string className = context.MergedJobDataMap["Job"].ToString();
            string configPath = context.MergedJobDataMap["ConfigPath"].ToString();

            var trigger = context.Trigger.Key.Name;
            var group = context.Trigger.Key.Group;

            var parameters = new Parameters(className, group, trigger, configPath, Program.ConnectionString);
            var activity = new Activity(parameters);
            await activity.Start();
        }
    }
}
