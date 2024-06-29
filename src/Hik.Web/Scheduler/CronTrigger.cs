using Quartz;
using System.Threading.Tasks;
using Job;

namespace Hik.Web.Scheduler
{
    class CronTrigger : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var trigger = context.Trigger.Key.Name;
            var group = context.Trigger.Key.Group;

            var parameters = new Parameters(group, trigger, Program.Environment);
            var activity = new Activity(parameters, Program.DBConfig, Program.EmailConfig, Program.AssemblyDirectory);
            await activity.Start();
        }
    }
}
