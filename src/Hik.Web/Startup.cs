using Autofac;
using Autofac.Features.Variance;
using Hik.Quartz;
using Hik.Web.Commands;
using Hik.Web.Queries;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpLogging;
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
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                });

            services.AddAuthentication("BasicAuthentication")
                .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>
                ("BasicAuthentication", null);

            services
                .AddAuthorization()
                .AddRazorPages();

            services.AddHttpLogging(options =>
            {
                options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders |
                                        HttpLoggingFields.RequestBody;
            });
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
                .UseAuthentication()
                .UseAuthorization()
                .UseStaticFiles()
                .UseRouting()
                .UseAuthorization()
                .UseHttpsRedirection()
                .UseHsts()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapRazorPages();
                });
        }
    }
}