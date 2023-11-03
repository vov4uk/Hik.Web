using Autofac;
using Autofac.Core;
using FluentFTP;
using Hik.Client.Abstraction;
using Hik.Client.Abstraction.Services;
using Hik.Client.FileProviders;
using Hik.Client.Infrastructure;
using Hik.DataAccess;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.Helpers.Abstraction;
using Job.Email;
using Job.Extensions;
using Job.Impl;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace Job
{
    [ExcludeFromCodeCoverage]
    public class JobFactory
    {
        public static async Task<IJobProcess> GetJobAsync(Parameters parameters)
        {
            IConfigurationRoot appConfig = new ConfigurationBuilder()
                .SetBasePath(parameters.ConfigPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{parameters.Environment}.json", optional: true, reloadOnChange: true)
                .Build();

            var loggerConfig = appConfig.GetSection("Serilog").Get<LoggerConfig>();
            var connection = appConfig.GetSection("DBConfiguration").Get<DbConfiguration>();

            JobTrigger trigger;
            ILogger logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("TriggerKey", parameters.TriggerKey)
                .Enrich.WithProperty("ActivityId", parameters.ActivityId)
                .WriteTo.Console()
                .WriteTo.File(
                 Path.Combine(loggerConfig.DefaultLogsPath, $"{parameters.TriggerKey}_.txt"),
                   rollingInterval: RollingInterval.Day,
                   fileSizeLimitBytes: 10 * 1024 * 1024,
                   retainedFileCountLimit: 2,
                   rollOnFileSizeLimit: true,
                   shared: true,
                   flushToDiskInterval: TimeSpan.FromSeconds(1))
                .WriteTo.Seq(loggerConfig.ServerUrl)
                .CreateLogger();

            IUnitOfWorkFactory unitOfWorkFactory = new UnitOfWorkFactory(connection);
            IHikDatabase db = new HikDatabase(unitOfWorkFactory, logger);

            trigger = await db.GetJobTriggerAsync(parameters.Group, parameters.TriggerKey);

            string configJson = trigger.Config;
            string className = trigger.ClassName;

            IEmailHelper email = new EmailHelper();

            var loggerParameter = new ResolvedParameter(
                                (pi, ctx) => pi.ParameterType == typeof(ILogger) && pi.Name == "logger",
                                (pi, ctx) => logger);

            switch (className)
            {
                case "ArchiveJob":
                    {
                        var worker = AppBootstrapper.Container.Resolve<IArchiveService>(loggerParameter);
                        return new ArchiveJob(trigger, worker, db, email, logger);
                    }
                case "GarbageCollectorJob":
                    {
                        var directory = AppBootstrapper.Container.Resolve<IDirectoryHelper>();
                        var files = AppBootstrapper.Container.Resolve<IFilesHelper>();
                        var provider = AppBootstrapper.Container.Resolve<IFileProvider>(loggerParameter);

                        return new GarbageCollectorJob(
                            trigger,
                            directory,
                            files,
                            provider,
                            db,
                            email,
                            logger);
                    }
                case "HikVideoDownloaderJob":
                    {
                        var factory = AppBootstrapper.Container.Resolve<IClientFactory>();
                        var factoryParameter = new ResolvedParameter(
                            (pi, ctx) => pi.ParameterType == typeof(IClientFactory) && pi.Name == "clientFactory",
                            (pi, ctx) => factory);
                        var service = AppBootstrapper.Container.Resolve<IHikVideoDownloaderService>(factoryParameter, loggerParameter);
                        return new HikVideoDownloaderJob(trigger, service, db, email, logger);
                    }
                case "HikPhotoDownloaderJob":
                    {
                        var factory = AppBootstrapper.Container.Resolve<IClientFactory>();
                        var factoryParameter = new ResolvedParameter(
                            (pi, ctx) => pi.ParameterType == typeof(IClientFactory) && pi.Name == "clientFactory",
                            (pi, ctx) => factory);
                        var service = AppBootstrapper.Container.Resolve<IHikPhotoDownloaderService>(factoryParameter, loggerParameter);
                        return new HikPhotoDownloaderJob(trigger, service, db, email, logger);
                    }
                default:
                    throw new ArgumentException($"No such type exist '{className}'");
            }
        }
    }
}
