using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Autofac;
using AutoMapper;
using Hik.Client.Service;

namespace Hik.Client.Infrastructure
{
    [ExcludeFromCodeCoverage]
    public static class AppBootstrapper
    {
        private static IContainer container = null;

        public static IContainer Container
        {
            get
            {
                if (container == null)
                {
                    container = ConfigureIoc();
                }

                return container;
            }
        }

        public static IContainer ConfigureIoc()
        {
            var builder = new ContainerBuilder();

            var referencedAssembliesNames = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
            List<Assembly> hikAssemblies = referencedAssembliesNames
                .Where(assembly => assembly.FullName.StartsWith("Hik."))
                .Select(x => Assembly.Load(x))
                .ToList();

            hikAssemblies.Add(Assembly.GetExecutingAssembly());
            RegisterAutoMapper(builder);
            builder.RegisterAssemblyTypes(hikAssemblies.ToArray()).AsImplementedInterfaces();
            builder.RegisterType<HikVideoDownloaderService>();
            builder.RegisterType<HikPhotoDownloaderService>();
            builder.RegisterType<DeleteSevice>();
            builder.RegisterType<ArchiveService>();
            builder.RegisterType<CleanUpService>();
            builder.RegisterType<YiVideoDownloaderService>();
            IContainer container = builder.Build();

            return container;
        }

        private static void RegisterAutoMapper(ContainerBuilder builder)
        {
            Action<IMapperConfigurationExpression> configureAutoMapper = x =>
            {
                x.AddProfile<HikConsoleProfile>();
            };

            builder.Register(context => new MapperConfiguration(configureAutoMapper))
                .SingleInstance()
                .AutoActivate()
                .AsSelf();

            builder.Register(ctx => ctx.Resolve<MapperConfiguration>().CreateMapper())
                .As<IMapper>()
                .InstancePerDependency();
        }
    }
}