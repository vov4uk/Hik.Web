using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Hik.DataAccess;
using Hik.Web.Scheduler;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hik.Web
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var isService = !(Debugger.IsAttached || args.Contains("--console"));

            StartupNet6(isService, args.Where(arg => arg != "--console").ToArray());
        }

        public static void StartupNet6(bool isService, string[] args)
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

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddRazorPages();
            builder.Services.AddDbContext<DataContext>(options =>
            {
                options.UseSqlite(config.GetConnectionString("HikConnectionString"), options =>
                {
                    options.MigrationsAssembly("Hik.DataAccess.dll");
                });
            });
            builder.Host.UseEnvironment(env);
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddEventLog();

            if (isService)
            {
                builder.Host.UseWindowsService();
                builder.WebHost.UseContentRoot(AssemblyDirectory);
            }

            var app = builder.Build();

            app.UseDeveloperExceptionPage();
            app.UseStaticFiles();
            app.UseRouting();
            app.MapRazorPages();
            app.Urls.Add($"http://+:{port}");

            var quartz = new QuartzStartup(config);

            app.Lifetime.ApplicationStarted.Register(quartz.Start);
            app.Lifetime.ApplicationStopping.Register(quartz.Stop);

            app.Run();
        }

        private static string AssemblyDirectory
        {
            get
            {
                var pathToExe = Process.GetCurrentProcess().MainModule?.FileName;
                return Path.GetDirectoryName(pathToExe);
            }
        }
    }
}
