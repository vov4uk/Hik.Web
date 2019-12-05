using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Autofac;
using HikConsole.Helpers;

namespace HikConsole.Infrastructure
{
    [ExcludeFromCodeCoverage]
    public static class AppBootstrapper
    {
        public static IContainer ConfigureIoc()
        {
            var builder = new ContainerBuilder();

            var referencedAssembliesNames = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
            List<Assembly> hikAssemblies = referencedAssembliesNames.Where(assembly => assembly.FullName.StartsWith("HikApi")).Select(x => Assembly.Load(x)).ToList();
            hikAssemblies.Add(Assembly.GetExecutingAssembly());

            builder.RegisterAssemblyTypes(hikAssemblies.ToArray()).AsImplementedInterfaces();
            builder.RegisterType<HikConfigurationManager>().SingleInstance();
            builder.RegisterType<Logger>().SingleInstance();
            IContainer container = builder.Build();

            return container;
        }
    }
}