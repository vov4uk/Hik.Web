using Autofac;
using Autofac.Features.Variance;
using Hik.Web.Commands;
using Hik.Web.Queries;
using Hik.Web.Scheduler;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Reflection;

namespace Hik.Web
{
    public class Startup
    {
        private readonly IConfiguration configuration;

        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            Assembly assembly = typeof(Startup).Assembly;
            Assembly[] projectsAssemblies = assembly.GetReferencedAssemblies()
                .Where(assemblyName => assemblyName.FullName.StartsWith("Hik.", StringComparison.InvariantCulture))
                .Select(Assembly.Load)
                .Union(new[] { assembly })
                .ToArray();

            services
                .AddDataBaseConfiguration(this.configuration)
                .AddAutoMapper(projectsAssemblies)
                .Configure<ApiBehaviorOptions>(options =>
                {
                    options.SuppressInferBindingSourcesForParameters = true;
                })
                .AddMvc()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                });

            services
                .AddRazorPages();

        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterAssemblyModules(typeof(Startup).Assembly);
            builder.RegisterModule(new CommandsModule());
            builder.RegisterModule(new QueriesModule());
            builder.RegisterSource(new ContravariantRegistrationSource());
        }

        public void Configure(IApplicationBuilder app,
            IHostApplicationLifetime lifetime)
        {
            var quartz = new QuartzStartup(configuration);

            lifetime.ApplicationStarted.Register(quartz.Start);
            lifetime.ApplicationStopping.Register(quartz.Stop);

            app.UseDeveloperExceptionPage()
            .UseStaticFiles()
            .UseRouting()
            .UseAuthorization()
            .UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}