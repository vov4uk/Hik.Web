using Autofac;
using Hik.Client.Abstraction;
using Hik.Client.FileProviders;
using Hik.Client.Infrastructure;
using Hik.DataAccess;
using Hik.DataAccess.Abstractions;
using Hik.DTO.Config;
using Hik.Helpers.Abstraction;
using Job.Email;
using Job.Extensions;
using Job.Impl;
using NLog;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Job
{
    [ExcludeFromCodeCoverage]
    public static class JobFactory
    {
        public static IJobProcess GetJob(Parameters parameters, ILogger logger, IEmailHelper email)
        {
            string trigger = $"{parameters.Group}.{parameters.TriggerKey}";
            IUnitOfWorkFactory unitOfWorkFactory = new UnitOfWorkFactory(new DbConfiguration { ConnectionString = parameters.ConnectionString });
            IHikDatabase db = new HikDatabase(unitOfWorkFactory);

            switch (parameters.ClassName)
            {
                case "Job.Impl.ArchiveJob, Job":
                    {
                        var config = HikConfigExtensions.GetConfig<ArchiveConfig>(parameters.ConfigFilePath);
                        IArchiveService worker = AppBootstrapper.Container.Resolve<IArchiveService>();
                        return new ArchiveJob(trigger, config, worker, db, email, logger);
                    }
                case "Job.Impl.GarbageCollectorJob, Job":
                    {
                        var config = HikConfigExtensions.GetConfig<GarbageCollectorConfig>(parameters.ConfigFilePath);
                        var directory = AppBootstrapper.Container.Resolve<IDirectoryHelper>();
                        var files = AppBootstrapper.Container.Resolve<IFilesHelper>();
                        var provider = AppBootstrapper.Container.Resolve<IFileProvider>();

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
                    {
                        var config = HikConfigExtensions.GetConfig<CameraConfig>(parameters.ConfigFilePath);
                        IHikVideoDownloaderService service = AppBootstrapper.Container.Resolve<IHikVideoDownloaderService>();
                        return new HikVideoDownloaderJob(trigger, config, service, db, email, logger);
                    }
                case "Job.Impl.HikPhotoDownloaderJob, Job":
                    {
                        var config = HikConfigExtensions.GetConfig<CameraConfig>(parameters.ConfigFilePath);
                        IHikPhotoDownloaderService service = AppBootstrapper.Container.Resolve<IHikPhotoDownloaderService>();
                        return new HikPhotoDownloaderJob(trigger, config, service, db, email, logger);
                    }
                case "Job.Impl.DbMigrationJob, Job":
                    {
                        var config = HikConfigExtensions.GetConfig<MigrationConfig>(parameters.ConfigFilePath);
                        var provider = AppBootstrapper.Container.Resolve<IFileProvider>();
                        return new DbMigrationJob(config, provider, db, email, logger);
                    }
                default:
                    throw new ArgumentException($"No such type exist '{parameters.ClassName}'");
            }
        }
    }
}
