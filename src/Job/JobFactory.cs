using Autofac;
using Autofac.Core;
using FluentFTP;
using Hik.Client.Abstraction;
using Hik.Client.Abstraction.Services;
using Hik.Client.FileProviders;
using Hik.Client.Infrastructure;
using Hik.DataAccess;
using Hik.DataAccess.Abstractions;
using Hik.DTO.Config;
using Hik.Helpers.Abstraction;
using Job.Email;
using Job.Extensions;
using Job.Impl;
using Serilog;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Job
{
    [ExcludeFromCodeCoverage]
    public static class JobFactory
    {
        public static IJobProcess GetJob(Parameters parameters, IEmailHelper email)
        {
            string trigger = $"{parameters.Group}.{parameters.TriggerKey}";
            ILogger logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("TriggerKey", parameters.TriggerKey)
                .Enrich.WithProperty("ActivityId", parameters.ActivityId)
                .WriteTo.Console()
                .WriteTo.File($"Logs\\{parameters.TriggerKey}_.txt", rollingInterval: RollingInterval.Day)
                .WriteTo.Seq("http://localhost:5341")
                .CreateLogger();

            IUnitOfWorkFactory unitOfWorkFactory = new UnitOfWorkFactory(new DbConfiguration { ConnectionString = parameters.ConnectionString });
            IHikDatabase db = new HikDatabase(unitOfWorkFactory, logger);

            var loggerParameter = new ResolvedParameter(
                                (pi, ctx) => pi.ParameterType == typeof(ILogger) && pi.Name == "logger",
                                (pi, ctx) => logger);

            switch (parameters.ClassName)
            {
                case "Job.Impl.ArchiveJob, Job":
                case "Archive":
                    {
                        var config = HikConfigExtensions.GetConfig<ArchiveConfig>(parameters.ConfigFilePath);
                        var worker = AppBootstrapper.Container.Resolve<IArchiveService>(loggerParameter);
                        return new ArchiveJob(trigger, config, worker, db, email, logger);
                    }
                case "Job.Impl.FtpUploader, Job":
                case "FtpUploader":
                    {
                        var config = HikConfigExtensions.GetConfig<FtpUploaderConfig>(parameters.ConfigFilePath);

                        var configParameter = new ResolvedParameter(
                            (pi, ctx) => pi.ParameterType == typeof(DeviceConfig) && pi.Name == "config",
                            (pi, ctx) => config?.FtpServer);

                        var ftpParameter = new ResolvedParameter(
                            (pi, ctx) => pi.ParameterType == typeof(IAsyncFtpClient) && pi.Name == "ftp",
                            (pi, ctx) => new AsyncFtpClient());

                        var client = AppBootstrapper.Container.Resolve<IUploaderClient>(configParameter, ftpParameter, loggerParameter);

                        var clientParameter = new ResolvedParameter(
                            (pi, ctx) => pi.ParameterType == typeof(IUploaderClient) && pi.Name == "ftp",
                            (pi, ctx) => client);

                        var worker = AppBootstrapper.Container.Resolve<IFtpUploaderService>(clientParameter, loggerParameter);

                        return new FtpUploaderJob(trigger, config, worker, db, email, logger);
                    }
                case "Job.Impl.GarbageCollectorJob, Job":
                case "GarbageCollector":
                    {
                        var config = HikConfigExtensions.GetConfig<GarbageCollectorConfig>(parameters.ConfigFilePath);
                        var directory = AppBootstrapper.Container.Resolve<IDirectoryHelper>();
                        var files = AppBootstrapper.Container.Resolve<IFilesHelper>();
                        var provider = AppBootstrapper.Container.Resolve<IFileProvider>(loggerParameter);

                        return new GarbageCollectorJob(
                            trigger,
                            config,
                            directory,
                            files,
                            provider,
                            db,
                            email,
                            logger);
                    }
                case "Job.Impl.HikVideoDownloaderJob, Job":
                case "HikVideoDownloader":
                    {
                        var config = HikConfigExtensions.GetConfig<CameraConfig>(parameters.ConfigFilePath);
                        var factory = AppBootstrapper.Container.Resolve<IClientFactory>();
                        var factoryParameter = new ResolvedParameter(
                            (pi, ctx) => pi.ParameterType == typeof(IClientFactory) && pi.Name == "clientFactory",
                            (pi, ctx) => factory);
                        var service = AppBootstrapper.Container.Resolve<IHikVideoDownloaderService>(factoryParameter, loggerParameter);
                        return new HikVideoDownloaderJob(trigger, config, service, db, email, logger);
                    }
                case "Job.Impl.HikPhotoDownloaderJob, Job":
                case "HikPhotoDownloader":
                    {
                        var config = HikConfigExtensions.GetConfig<CameraConfig>(parameters.ConfigFilePath);
                        var factory = AppBootstrapper.Container.Resolve<IClientFactory>();
                        var factoryParameter = new ResolvedParameter(
                            (pi, ctx) => pi.ParameterType == typeof(IClientFactory) && pi.Name == "clientFactory",
                            (pi, ctx) => factory);
                        var service = AppBootstrapper.Container.Resolve<IHikPhotoDownloaderService>(factoryParameter, loggerParameter);
                        return new HikPhotoDownloaderJob(trigger, config, service, db, email, logger);
                    }
                case "Job.Impl.DbMigrationJob, Job":
                case "DbMigration":
                    {
                        var config = HikConfigExtensions.GetConfig<MigrationConfig>(parameters.ConfigFilePath);
                        var provider = AppBootstrapper.Container.Resolve<IFileProvider>(loggerParameter);
                        return new DbMigrationJob(config, provider, db, email, logger);
                    }
                default:
                    throw new ArgumentException($"No such type exist '{parameters.ClassName}'");
            }
        }
    }
}
