using Autofac;
using Job.Commands;
using MediatR.Extensions.Autofac.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Hik.Web
{
    public static class AutofacConfig
    {
        public static IContainer Container { get; private set; }

        public static void RegisterConfiguration(IConfiguration configuration)
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(configuration).SingleInstance();
            builder.RegisterMediatR(typeof(ActivityCommand).Assembly);
            Container = builder.Build();
        }
    }
}