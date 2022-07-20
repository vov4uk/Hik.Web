using Job;
using Job.Email;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace JobHost
{
    [ExcludeFromCodeCoverage]
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            var email = new EmailHelper();
            var parameters = Parameters.Parse(args);
            var Logger = new LoggerFactory()
                .AddFile($"logs\\{parameters.TriggerKey}.txt")
                .AddSeq()
                .CreateLogger(parameters.TriggerKey);
            try
            {
                Logger.LogInformation("Parameters resolved. {parameters}. Activity started execution.", parameters);

                IJobProcess job = JobFactory.GetJob(parameters, Logger, email);

                await job.ExecuteAsync();

                Logger.LogInformation("Activity completed execution.");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "JobHost. Exception");
                email.Send(exception);
                Environment.ExitCode = -1;
            }
        }
    }
}