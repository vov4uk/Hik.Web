using System;
using System.Collections.Specialized;
using Microsoft.Extensions.Configuration;
using Quartz.Impl;

namespace Hik.Quartz.Contracts.Options
{
    /// <summary>
    /// More details:
    /// https://github.com/quartznet/quartznet/blob/master/src/Quartz/Impl/StdSchedulerFactory.cs
    /// </summary>
    public class QuartzOption
    {
        public QuartzOption(IConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var section = config.GetSection("quartz");
            section.Bind(this);
        }

        public Scheduler Scheduler { get; set; }

        public ThreadPool ThreadPool { get; set; }

        public Plugin Plugin { get; set; }

        public JobStore JobStore { get; set; }

        public NameValueCollection ToProperties => new NameValueCollection
        {
            [StdSchedulerFactory.PropertySchedulerInstanceName] = Scheduler?.InstanceName,
            [StdSchedulerFactory.PropertyThreadPoolType] = ThreadPool?.Type,
            ["quartz.threadPool.threadCount"] = ThreadPool?.ThreadCount.ToString(),
            ["quartz.plugin.jobInitializer.type"] = Plugin?.JobInitializer?.Type,
            ["quartz.plugin.jobInitializer.fileNames"] = Plugin?.JobInitializer?.FileNames,
            ["quartz.jobStore.type"] = JobStore?.Type
        };
    }
}