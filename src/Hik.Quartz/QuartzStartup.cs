using Hik.Quartz.Contracts.Options;
using Microsoft.Extensions.Configuration;
using Quartz;
using Quartz.Impl;
using System;

namespace Hik.Quartz
{
    public class QuartzStartup
    {
        private readonly IConfiguration configuration;
        private IScheduler scheduler;
        public QuartzStartup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void Start()
        {
            if (scheduler != null)
            {
                throw new InvalidOperationException("Already started.");
            }

            var properties = new QuartzOption(configuration).ToProperties();

            var schedulerFactory = new StdSchedulerFactory(properties);
            scheduler = schedulerFactory.GetScheduler().GetAwaiter().GetResult();
            scheduler.Start().Wait();
        }

        public void Stop()
        {
            if (scheduler == null)
            {
                return;
            }

            if (scheduler.Shutdown(true).Wait(30000))
            {
                scheduler = null;
            }
            else
            {
                // jobs didn't exit in timely fashion - log a warning...
            }
        }
    }
}
