using Autofac;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Module = Autofac.Module;

namespace Hik.Web
{
    internal sealed class Dependencies : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<Mediator>()
                .As<IMediator>()
                .InstancePerLifetimeScope();

            var referencedAssembliesNames = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
            List<Assembly> hikAssemblies = referencedAssembliesNames
                .Where(assembly => assembly.FullName.StartsWith("Hik."))
                .Select(Assembly.Load)
                .ToList();

            hikAssemblies.Add(Assembly.GetExecutingAssembly());
            builder.RegisterAssemblyTypes(hikAssemblies.ToArray()).AsImplementedInterfaces();
        }
    }
}
