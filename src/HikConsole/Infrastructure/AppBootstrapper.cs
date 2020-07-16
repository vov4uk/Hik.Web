using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Autofac;
using AutoMapper;
using HikConsole.Helpers;
using HikConsole.Scheduler;

namespace HikConsole.Infrastructure
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
                .Where(assembly => assembly.FullName.StartsWith("HikApi") || assembly.FullName.StartsWith("HikConsole"))
                .Select(x => Assembly.Load(x))
                .ToList();

            hikAssemblies.Add(Assembly.GetExecutingAssembly());
            RegisterAutoMapper(builder);
            builder.RegisterAssemblyTypes(hikAssemblies.ToArray()).AsImplementedInterfaces();
            builder.RegisterType<HikConfig>().SingleInstance();
            builder.RegisterType<HikDownloader>();
            builder.RegisterType<DeleteArchiving>();
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