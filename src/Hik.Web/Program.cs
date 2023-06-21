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

namespace Hik.Web
{
    public static class Program
    {
        private const string ConsoleParameter = "--console";
        public static string Version { get; set; }
        public static string ConnectionString { get; set; }

        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("Logs\\hikweb_.txt", rollingInterval: RollingInterval.Day)
                .WriteTo.Seq("http://localhost:5341")
                .CreateLogger();

            var isService = !(Debugger.IsAttached || args.Contains(ConsoleParameter));
            var builder = await CreateHostBuilder(isService, args.Where(arg => arg != ConsoleParameter).ToArray());

            var host = builder.Build();

            Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            host.Run();
        }

        public static async Task<IHostBuilder> CreateHostBuilder(bool isService, string[] args)
        {
            Directory.SetCurrentDirectory(AssemblyDirectory);

            var env = isService ? "Production" : "Development";
            var config = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
                .AddCommandLine(args)
                .Build();

            var port = config.GetSection("Hosting:Port").Value;

            ConnectionString = config.GetSection("DBConfiguration").GetSection("ConnectionString").Value;

            await MigrationTools.RunMigration(ConnectionString);

            var host = Host.CreateDefaultBuilder(args)
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

        private static string AssemblyDirectory
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