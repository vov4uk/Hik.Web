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
                logger.Info($"JobHost. Parameters resolved. {parameters}");
                logger.Info("JobHost. Activity started execution.");

                Type jobType = Type.GetType(parameters.ClassName);

                if (jobType == null)
                {
                    throw new ArgumentException($"No such type exist '{parameters.ClassName}'");
                }

                Job.Impl.JobProcessBase job = (Job.Impl.JobProcessBase)Activator.CreateInstance(
                    jobType, 
                    $"{parameters.Group}.{parameters.TriggerKey}", 
                    parameters.ConfigFilePath, 
                    parameters.ConnectionString, 
                    parameters.ActivityId);
                job.Parameters = parameters;
                job.ExecuteAsync().GetAwaiter().GetResult();
                logger.Info("JobHost. Activity completed execution.");
            }
            catch (Exception exception)
            {
                logger.Error($"JobHost. Exception : {exception}");
                Environment.ExitCode = -1;
            }
        }
    }
}