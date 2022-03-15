using Autofac;
using Job.Commands;
using Microsoft.Extensions.Configuration;
using MediatR.Extensions.Autofac.DependencyInjection;

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
