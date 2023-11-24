using Autofac;
using Autofac.Core;
using Hik.Client.Abstraction;
using Hik.Client.Abstraction.Services;
using Hik.Client.FileProviders;
using Hik.Client.Infrastructure;
using Hik.DataAccess;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.Helpers.Abstraction;
using Job.Email;
using Job.Impl;
using Serilog;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Job
{
    [ExcludeFromCodeCoverage]
    public static class JobFactory
    {
        public static async Task<IJobProcess> GetJobAsync(Parameters parameters, DbConfiguration connection, ILogger logger)
        {
            JobTrigger trigger;

            IUnitOfWorkFactory unitOfWorkFactory = new UnitOfWorkFactory(connection);
            HikDatabase db = new HikDatabase(unitOfWorkFactory, logger);

            trigger = await db.GetJobTriggerAsync(parameters.Group, parameters.TriggerKey);

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
