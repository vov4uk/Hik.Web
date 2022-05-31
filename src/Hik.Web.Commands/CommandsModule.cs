using Autofac;
using MediatR;
using System.Diagnostics.CodeAnalysis;

namespace Hik.Web.Commands
{
    [ExcludeFromCodeCoverage]
    public class CommandsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            System.Reflection.Assembly commandsAssembly = typeof(CommandsModule).Assembly;
            builder
                .RegisterAssemblyTypes(commandsAssembly)
                .AsClosedTypesOf(typeof(IRequestHandler<>))
                .AsImplementedInterfaces();

            builder
                .RegisterAssemblyTypes(commandsAssembly)
                .AsClosedTypesOf(typeof(IRequestHandler<,>))
                .AsImplementedInterfaces();
        }
    }
}