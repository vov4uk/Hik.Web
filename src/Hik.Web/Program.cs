using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog.Events;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Hik.Web
{
    public static class Program
    {
        private const string ConsoleParameter = "--console";
        public static string Version { get; set; }
        public static string ConnectionString { get; set; }

        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                // Filter out ASP.NET Core infrastructre logs that are Information and below
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("Logs\\hikweb-{Date}.txt")
                .WriteTo.Seq("http://localhost:5341")
                .CreateLogger();

            var isService = !(Debugger.IsAttached || args.Contains(ConsoleParameter));
            var builder = CreateHostBuilder(isService, args.Where(arg => arg != ConsoleParameter).ToArray());

            var host = builder.Build();

            Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(bool isService, string[] args)
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