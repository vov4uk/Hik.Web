using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;

namespace Hik.Web
{
    public class QuartzStartup
    {
        private IScheduler scheduler;

        public void Start()
        {
            if (scheduler != null)
            {
                throw new InvalidOperationException("Already started.");
            }

            string configFileName = "quartz_jobs.xml";
#if DEBUG
            configFileName = configFileName.Replace(".xml", ".debug.xml");
#endif

            var properties = new NameValueCollection
            {

                ["quartz.serializer.type"] = "json",
                ["quartz.scheduler.instanceName"] = "Hik.Web",
                ["quartz.threadPool.threadCount"] = "3",
                ["quartz.jobStore.type"] = "Quartz.Simpl.RAMJobStore, Quartz",
                ["quartz.plugin.xml.type"] = "Quartz.Plugin.Xml.XMLSchedulingDataProcessorPlugin, Quartz.Plugins",
                ["quartz.plugin.xml.fileNames"] = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location), configFileName),
            };

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
