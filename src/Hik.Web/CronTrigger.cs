using System.Threading.Tasks;
using Autofac;
using Job;
using Microsoft.Extensions.Configuration;

namespace Hik.Web
{
    class CronTrigger : Quartz.IJob
    {
        public async Task Execute(Quartz.IJobExecutionContext context)
        {
            string className = context.MergedJobDataMap["Job"].ToString();
            string configPath = context.MergedJobDataMap["ConfigPath"].ToString();

            var configuration = AutofacConfig.Container.Resolve<IConfiguration>();

            IConfigurationSection connStrings = configuration.GetSection("ConnectionStrings");
            string defaultConnection = connStrings.GetSection("HikConnectionString").Value;

            var trigger = context.Trigger.Key.Name;
            var group = context.Trigger.Key.Group;

            var parameters = new Parameters(className, group, trigger, configPath, defaultConnection);
            var activity = new Activity(parameters);
            await activity.Start();
        }
    }
}
