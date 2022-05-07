using Hik.DataAccess;
using Hik.DataAccess.Abstractions;
using Job;
using Job.Email;
using NLog;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace JobHost
{
    [ExcludeFromCodeCoverage]
    internal static class Program
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private static async Task Main(string[] args)
        {
            var email = new EmailHelper();
            try
            {
                var parameters = Parameters.Parse(args);
                var prevActivityId = System.Diagnostics.Trace.CorrelationManager.ActivityId;
                System.Diagnostics.Trace.CorrelationManager.ActivityId = parameters.ActivityId;
                Logger.Info($"JobHost. Parameters resolved. {parameters}. Activity started execution.");

                Type jobType = Type.GetType(parameters.ClassName) ?? throw new ArgumentException($"No such type exist '{parameters.ClassName}'");

                IUnitOfWorkFactory unitOfWorkFactory = new UnitOfWorkFactory(parameters.ConnectionString);
                IHikDatabase db = new HikDatabase(unitOfWorkFactory);
                Job.Impl.JobProcessBase job = (Job.Impl.JobProcessBase)Activator.CreateInstance(
                    jobType,
                    $"{parameters.Group}.{parameters.TriggerKey}",
                    parameters.ConfigFilePath,
                    db,
                    email,
                    parameters.ActivityId);

                await job.ExecuteAsync();

                Logger.Info("JobHost. Activity completed execution.");

                System.Diagnostics.Trace.CorrelationManager.ActivityId = prevActivityId;
            }
            catch (Exception exception)
            {
                Logger.Error($"JobHost. Exception : {exception}");
                email.Send(exception);
                Environment.ExitCode = -1;
            }
        }
    }
}