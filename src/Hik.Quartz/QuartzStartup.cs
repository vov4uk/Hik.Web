using Hik.Quartz.Contracts.Options;
using Hik.Quartz.Contracts.Xml;
using Hik.Quartz.Extensions;
using Microsoft.Extensions.Configuration;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

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
            var logsPath = configuration.GetSection("Serilog:DefaultLogsPath").Value;
            LogProvider.SetCurrentLogProvider(new ConsoleLogProvider(logsPath));

            var options = new QuartzOption(configuration);

            var schedulerFactory = new StdSchedulerFactory(options.ToProperties);
            scheduler = schedulerFactory.GetScheduler().GetAwaiter().GetResult();
            scheduler.Start().Wait();
        }

        public static void InitializeJobs(IConfiguration configuration, IReadOnlyCollection<Cron> triggers)
        {
            if (configuration != null)
            {
                var options = new QuartzOption(configuration);
                GenerateQuartzXml(options.Plugin.JobInitializer.FileNames, triggers);
            }
        }

        private static void GenerateQuartzXml(string xmlFilePath, IReadOnlyCollection<Cron> triggers)
        {
            var serializer = new XmlSerializer(typeof(JobSchedulingData));
            var data = new JobSchedulingData();

            data.Schedule.Trigger.AddRange(triggers.Select(x => new Trigger { Cron = x }));

            using (StringWriter writer = new StringWriter())
            {
                serializer.Serialize(writer, data);
                File.WriteAllText(xmlFilePath, writer.ToString());
            }
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
