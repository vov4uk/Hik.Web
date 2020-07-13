using System;
using Job;
using NLog;

namespace JobHost
{
    class Program
    {
        private static ILogger logger = LogManager.GetCurrentClassLogger();
        static void Main(string[] args)
        {
            try
            {
                var parameters = Parameters.Parse(args);
                System.Diagnostics.Trace.CorrelationManager.ActivityId = parameters.ActivityId;
                logger.Info("Parameters resolved. Activity started execution.");

                Type jobType = Type.GetType(parameters.ClassName);

                Job.Impl.JobProcessBase job = (Job.Impl.JobProcessBase)Activator.CreateInstance(jobType, parameters.TriggerKey, parameters.ConfigFilePath, parameters.ConnectionString, parameters.ActivityId);
                job.Parameters = parameters;
                job.ExecuteAsync().GetAwaiter().GetResult();
                logger.Info("Activity completed execution.");
            }
            catch (Exception exception)
            {
                logger.Error(exception);
                Environment.ExitCode = -1;
            }
        }
    }
}