using Job;
using Job.Email;
using NLog;
using System;
using System.Threading.Tasks;

namespace JobHost
{
    internal static class Program
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private static async Task Main(string[] args)
        {
            try
            {
                var parameters = Parameters.Parse(args);
                System.Diagnostics.Trace.CorrelationManager.ActivityId = parameters.ActivityId;
                Logger.Info($"JobHost. Parameters resolved. {parameters}. Activity started execution.");

                Type jobType = Type.GetType(parameters.ClassName) ?? throw new ArgumentException($"No such type exist '{parameters.ClassName}'");

                Job.Impl.JobProcessBase job = (Job.Impl.JobProcessBase)Activator.CreateInstance(
                    jobType,
                    $"{parameters.Group}.{parameters.TriggerKey}",
                    parameters.ConfigFilePath,
                    parameters.ConnectionString,
                    parameters.ActivityId);

                await job.ExecuteAsync();

                Logger.Info("JobHost. Activity completed execution.");
            }
            catch (Exception exception)
            {
                Logger.Error($"JobHost. Exception : {exception}");
                EmailHelper.Send(exception);
                Environment.ExitCode = -1;
            }
        }
    }
}