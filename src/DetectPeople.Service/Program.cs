using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DetectPeople.Service
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
            return 0;
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var isService = !(Debugger.IsAttached || args.Contains("--console"));

            var service = Host.CreateDefaultBuilder(args)
                .ConfigureLogging(configureLogging => configureLogging.AddFilter<EventLogLoggerProvider>(level => level >= LogLevel.Information))
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<PeopleDetectWorker>()
                            .Configure<EventLogSettings>(config =>
                            {
                                config.LogName = "Person Detect Service";
                                config.SourceName = "Person Detect Service Source";
                            });
                });

            if (isService)
            {
                service.UseWindowsService();
            }

            return service;
        }

    }
}
