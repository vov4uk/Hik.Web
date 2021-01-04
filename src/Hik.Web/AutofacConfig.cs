using Autofac;
using Microsoft.Extensions.Configuration;

namespace Hik.Web
{
    public class AutofacConfig
    {
        public static IContainer Container { get; private set; }

        public static void RegisterConfiguration(IConfiguration configuration)
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(configuration).SingleInstance();
            Container = builder.Build();
        }
    }
}
