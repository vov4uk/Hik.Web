using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Hik.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // https://dotnetcoretutorials.com/2018/09/12/hosting-an-asp-net-core-web-application-as-a-windows-service/
            var isService = !(Debugger.IsAttached || args.Contains("--console"));
            var builder = CreateHostBuilder(isService, args.Where(arg => arg != "--console").ToArray());

            if (isService)
            {
                var pathToExe = Process.GetCurrentProcess().MainModule?.FileName;
                var pathToContentRoot = Path.GetDirectoryName(pathToExe);
                builder.UseContentRoot(pathToContentRoot);
            }

            var host = builder.Build();

            if (isService)
            {
#pragma warning disable CA1416 // Validate platform compatibility
                host.RunAsService();
#pragma warning restore CA1416 // Validate platform compatibility
            }
            else
            {
                host.Run();
            }
        }

        public static IWebHostBuilder CreateHostBuilder(bool isService, string[] args)
        {
            var env = isService ? "Production" : "Development";
            var config = new ConfigurationBuilder()
            .SetBasePath(AssemblyDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
            .AddCommandLine(args)
            .Build();

            AutofacConfig.RegisterConfiguration(config);

            var port = config.GetSection("Hosting:Port").Value;

            return new WebHostBuilder()
                .UseKestrel()
                .UseEnvironment(env)
                .UseConfiguration(config)
                .UseIISIntegration()
                .UseStartup<Startup>()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddEventLog();
                    logging.AddConsole();
                })
                .UseUrls($"http://+:{port}");
        }

        private static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().Location;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
    }
}
