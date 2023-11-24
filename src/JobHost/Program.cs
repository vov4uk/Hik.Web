using Hik.DataAccess;
using Hik.DTO.Config;
using Job;
using Job.Email;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using Serilog.Core;

namespace JobHost
{
    [ExcludeFromCodeCoverage]
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            EmailHelper email = null;
            Logger logger = null;
            try
            {
                email = new EmailHelper();
                var parameters = Parameters.Parse(args);

                IConfigurationRoot appConfig = new ConfigurationBuilder()
                    .SetBasePath(Environment.CurrentDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{parameters.Environment}.json", optional: true, reloadOnChange: true)
                    .Build();

                var loggerConfig = appConfig.GetSection("Serilog").Get<LoggerConfig>();
                var connection = appConfig.GetSection("DBConfiguration").Get<DbConfiguration>();

                logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("ActivityId", $"{parameters.TriggerKey}_{parameters.ActivityId}")
                    .WriteTo.Console()
                    .WriteTo.File(
                     Path.Combine(loggerConfig.DefaultLogsPath, $"{parameters.TriggerKey}_.txt"),
                       rollingInterval: RollingInterval.Day,
                       fileSizeLimitBytes: 10 * 1024 * 1024,
                       retainedFileCountLimit: 2,
                       rollOnFileSizeLimit: true,
                       shared: true,
                       flushToDiskInterval: TimeSpan.FromSeconds(1))
                    .WriteTo.Seq(loggerConfig.ServerUrl,
                                 period : TimeSpan.FromSeconds(1))
                    .CreateLogger();


                IJobProcess job = await JobFactory.GetJobAsync(parameters, connection, logger);

                await job.ExecuteAsync();
            }
            catch (Exception ex)
            {
                logger?.Error(ex.ToStringDemystified());
                email?.Send(ex.Message);
                Environment.ExitCode = -1;
            }

            logger?.Dispose();
            await Task.Delay(2000); // to write logs
        }
    }
}