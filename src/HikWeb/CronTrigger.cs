using System;
using System.Configuration;
using System.Threading.Tasks;
using Autofac;
using Job;
using Microsoft.Extensions.Configuration;

namespace HikWeb
{
    class CronTrigger : Quartz.IJob
    {
        public async Task Execute(Quartz.IJobExecutionContext context)
        {
            string className = context.MergedJobDataMap["Job"].ToString();
            string configPath = context.MergedJobDataMap["ConfigPath"].ToString();
            var jobType = (JobType)Enum.Parse(typeof(JobType), context.MergedJobDataMap["JobType"].ToString());

            var configuration = AutofacConfig.Container.Resolve<IConfiguration>();

            IConfigurationSection connStrings = configuration.GetSection("ConnectionStrings");
            string defaultConnection = connStrings.GetSection("HikConnectionString").Value;

            var trigger = context.Trigger.Key.Name;

            var parameters = new Parameters(className, trigger, configPath, defaultConnection);
            var activity = new Activity(parameters);
            await activity.Start();
        }
    }
}
