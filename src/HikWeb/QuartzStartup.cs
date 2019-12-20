using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;

namespace HikWeb
{
    // Responsible for starting and gracefully stopping the scheduler.
    public class QuartzStartup
    {
        private IScheduler _scheduler; // after Start, and until shutdown completes, references the scheduler object

        // starts the scheduler, defines the jobs and the triggers
        public void Start()
        {
            if (_scheduler != null)
            {
                throw new InvalidOperationException("Already started.");
            }

            var properties = new NameValueCollection
            {
                // json serialization is the one supported under .NET Core (binary isn't)
                ["quartz.serializer.type"] = "json",
                ["quartz.scheduler.instanceName"] = "HikWeb",

                // set thread pool info
                ["quartz.threadPool.threadCount"] = "3",
                ["quartz.threadPool.threadPriority"] = "Normal",

                // the following setup of job store is just for example and it didn't change from v2
                ["quartz.jobStore.type"] = "Quartz.Simpl.RAMJobStore, Quartz",
                // job initialization plugin handles our xml reading, without it defaults are used
                ["quartz.plugin.xml.type"] = "Quartz.Plugin.Xml.XMLSchedulingDataProcessorPlugin, Quartz.Plugins",
                ["quartz.plugin.xml.fileNames"] = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "quartz_jobs.xml"),
            };

            var schedulerFactory = new StdSchedulerFactory(properties);
            _scheduler = schedulerFactory.GetScheduler().Result;
            _scheduler.Start().Wait();
        }

        // initiates shutdown of the scheduler, and waits until jobs exit gracefully (within allotted timeout)
        public void Stop()
        {
            if (_scheduler == null)
            {
                return;
            }

            // give running jobs 30 sec (for example) to stop gracefully
            if (_scheduler.Shutdown(waitForJobsToComplete: true).Wait(30000))
            {
                _scheduler = null;
            }
            else
            {
                // jobs didn't exit in timely fashion - log a warning...
            }
        }
    }
}
