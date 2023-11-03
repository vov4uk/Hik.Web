using Hik.Quartz.Contracts.Options;
using Microsoft.Extensions.Configuration;
using Quartz;
using Quartz.Impl;
using System.Threading.Tasks;

namespace Hik.Quartz.Services
{
    public class CronService : ICronService
    {
        public async Task RestartSchedulerAsync(IConfiguration configuration)
        {
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            var currentScheduler = await schedulerFactory.GetScheduler("default");
            await currentScheduler.Shutdown(false);

            var options = new QuartzOption(configuration);

            schedulerFactory = new StdSchedulerFactory(options.ToProperties);
            var scheduler = await schedulerFactory.GetScheduler();
            await scheduler.Start();
        }
    }
}
