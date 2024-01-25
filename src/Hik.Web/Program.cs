//#define USE_AUTHORIZATION
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Hik.DataAccess.SQL;
using System.Threading.Tasks;
using Serilog.Events;
using Hik.DataAccess;
using Hik.DTO.Config;

namespace Hik.Web
{
    public static class Program
    {
        private const string ConsoleParameter = "--console";
        private const string AppSettings = "appsettings.json";

        internal static string Version { get; set; }

        internal static string Environment { get; set; }

        internal static DbConfiguration DBConfig { get; set; }

        public static async Task Main(string[] args)
        {
            Console.WriteLine($"AssemblyDirectory : {AssemblyDirectory}");

            var isService = !(Debugger.IsAttached || args.Contains(ConsoleParameter));
            Environment = isService ? "Production" : "Development";

            string envSettings = $"appsettings.{Environment}.json";

            Console.WriteLine($"Environment : {Environment}");
            Console.WriteLine($"appsettings.json : {File.Exists(Path.Combine(AssemblyDirectory, AppSettings))}");
            Console.WriteLine($"{envSettings} : {File.Exists(Path.Combine(AssemblyDirectory, envSettings))}");

            IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(AssemblyDirectory)
                .AddJsonFile(AppSettings, optional: true, reloadOnChange: true)
                .AddJsonFile(envSettings, optional: true, reloadOnChange: true)
                .Build();

            var loggerConfig = config.GetSection("Serilog").Get<LoggerConfig>();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(
                   Path.Combine(loggerConfig.DefaultLogsPath,"hikweb_.txt"),
                   rollingInterval: RollingInterval.Day,
                   fileSizeLimitBytes: 10 * 1024 * 1024,
                   retainedFileCountLimit: 2,
                   rollOnFileSizeLimit: true,
                   shared: true,
                   flushToDiskInterval: TimeSpan.FromSeconds(1))
                   .WriteTo.Seq(loggerConfig.ServerUrl,
                                 period: TimeSpan.FromSeconds(1))
                .CreateLogger();

#if USE_AUTHORIZATION
            Log.Information("USE_AUTHORIZATION");
#endif
            DBConfig = config.GetSection("DBConfiguration").Get<DbConfiguration>();

            await MigrationTools.RunMigration(DBConfig);

            var builder = CreateHostBuilder(isService, config);

            var host = builder.Build();

            var version = Assembly.GetExecutingAssembly().GetName().Version;

            var buildDate = new DateTime(2000, 1, 1)
               .AddDays(version.Build).AddSeconds(version.Revision * 2);

            Version = $"{version} ({buildDate})";

            Log.Information($"App started - version {Version}");
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(bool isService, IConfigurationRoot config)
        {
            var port = config.GetSection("Hosting:Port").Value;

            var host = Host.CreateDefaultBuilder()
                .UseSerilog()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                    .UseKestrel(
                        options =>
                        {
                            const int MaxUrlSizeBytes = 32768;
                            options.Limits.MaxRequestBufferSize = MaxUrlSizeBytes;
                            options.Limits.MaxRequestLineSize = MaxUrlSizeBytes;
                        })
#if USE_AUTHORIZATION
                    .UseUrls($"https://+:{port}")
                    .ConfigureKestrel(kestrel =>
                    {
                        kestrel.ListenAnyIP(int.Parse(port), portOptions =>
                        {
                            portOptions.UseHttps(h =>
                            {
                                h.UseLettuceEncrypt(kestrel.ApplicationServices);
                            });
                        });
                    })
#else
                    .UseUrls($"http://+:{port}")
#endif
                    .UseStartup<Startup>();
                });

            if (isService)
            {
                host.UseWindowsService();
            }

            return host;
        }

        internal static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().Location;
                UriBuilder uri = new(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
    }
}