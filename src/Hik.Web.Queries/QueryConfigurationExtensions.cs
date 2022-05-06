using Hik.DataAccess;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hik.Web.Queries
{
    public static class QueryConfigurationExtensions
    {
        public static IServiceCollection AddDataBaseConfiguration(this IServiceCollection services, IConfiguration configuration)
            => services.AddOptions()
                .Configure<DbConfiguration>(configuration, "DBConfiguration");

        private static IServiceCollection Configure<T>(this IServiceCollection services, IConfiguration configuration, string path)
            where T : class, new()
            => services
            .Configure<T>(configuration.GetSection(path))
            .AddTransient(Register<T>);

        private static T Register<T>(IServiceProvider provider)
            where T : class, new()
            => provider.GetService<IOptionsSnapshot<T>>().Value;
    }
}
