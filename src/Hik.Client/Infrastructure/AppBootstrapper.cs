using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Autofac;
using AutoMapper;

namespace Hik.Client.Infrastructure
{
    [ExcludeFromCodeCoverage]
    public static class AppBootstrapper
    {
        private static IContainer container = null;

        public static IContainer Container => container ??= ConfigureIoc();

        public static IContainer ConfigureIoc()
        {
            var builder = new ContainerBuilder();

            var referencedAssembliesNames = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
            List<Assembly> hikAssemblies = referencedAssembliesNames
                .Where(assembly =>
                  assembly.FullName.StartsWith("Hik.") ||
                  assembly.FullName.StartsWith("Dahua."))
                .Select(Assembly.Load)
                .ToList();

            hikAssemblies.Add(Assembly.GetExecutingAssembly());
            RegisterAutoMapper(builder);
            builder.RegisterAssemblyTypes(hikAssemblies.ToArray()).AsImplementedInterfaces();

            return builder.Build();
        }

        private static void RegisterAutoMapper(ContainerBuilder builder)
        {
            void AutoMapper(IMapperConfigurationExpression x)
            {
                x.AddProfile<HikConsoleProfile>();
            }

            builder.Register(context => new MapperConfiguration(AutoMapper))
                .SingleInstance()
                .AutoActivate()
                .AsSelf();

            builder.Register(ctx => ctx.Resolve<MapperConfiguration>().CreateMapper())
                .As<IMapper>()
                .InstancePerDependency();
        }
    }
}