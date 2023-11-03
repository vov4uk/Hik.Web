using Autofac;
using Autofac.Features.Variance;
using Hik.DataAccess;
using Hik.Helpers;
using Hik.Quartz;
using Hik.Quartz.Services;
using Hik.Web.Commands;
using Hik.Web.Queries;
using Hik.Web.Queries.QuartzTriggers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using Hik.Quartz.Contracts.Xml;

namespace Hik.Web
{
    public class Startup
    {
        private const string BasicAuthentication = "BasicAuthentication";
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
                });
#if USE_AUTHORIZATION
            services.AddAuthentication(BasicAuthentication)
                .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>
                (BasicAuthentication, null);
            services.AddAuthorization();
#endif
            services.AddRazorPages();

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
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

            var triggers = GetTriggersFromDataBase().GetAwaiter().GetResult();
            QuartzStartup.InitializeJobs(this.configuration, triggers);

            lifetime.ApplicationStarted.Register(quartz.Start);
            lifetime.ApplicationStopping.Register(quartz.Stop);

            app.UseDeveloperExceptionPage()
                .UseStaticFiles()
                .UseSerilogRequestLogging()
                .UseRouting();

#if USE_AUTHORIZATION
            app.UseAuthentication()
                .UseAuthorization()
                .UseHttpsRedirection()
                .UseHsts();
#endif
            app.UseEndpoints(endpoints =>
                {
#if USE_AUTHORIZATION
                 endpoints.MapRazorPages()
                    .RequireAuthorization();
#else
                    endpoints.MapRazorPages();
#endif
                });
        }

        internal async Task<IReadOnlyCollection<Cron>> GetTriggersFromDataBase()
        {
            var connection = this.configuration.GetSection("DBConfiguration").Get<DbConfiguration>();
            QuartzTriggersQueryHandler handler = new QuartzTriggersQueryHandler(new UnitOfWorkFactory(connection));
            var triggersDto = await handler.Handle(new QuartzTriggersQuery { ActiveOnly = true }, CancellationToken.None) as QuartzTriggersDto;

            return triggersDto.Triggers.Where(x =>
            !string.IsNullOrEmpty(x.Name) &&
            !string.IsNullOrEmpty(x.Group) &&
            !string.IsNullOrEmpty(x.CronExpression) &&
            !string.IsNullOrEmpty(x.Description))
                .Select(x =>x.ToCron())
                .ToList();
        }
    }
}