using Autofac;
using Autofac.Features.Variance;
using Hik.DataAccess;
using Hik.Quartz;
using Hik.Web.Commands;
using Hik.Web.Queries;
using Hik.Web.Queries.QuartzTriggers;
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
using FluentValidation.AspNetCore;
using FluentValidation;
#if USE_AUTHORIZATION
using idunno.Authentication.Basic;
using System.Security.Claims;
#endif

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
                });
#if USE_AUTHORIZATION

            var allowedUsers = configuration.GetSection("BasicAuthentication:AllowedUsers").Get<string[]>();
            services.AddAuthentication(BasicAuthenticationDefaults.AuthenticationScheme)
            .AddBasic(options =>
            {
                options.Realm = "hikweb";
                options.Events = new BasicAuthenticationEvents
                {
                    OnValidateCredentials = context =>
                    {
                        if (allowedUsers.Contains($"{context.Username}:{context.Password}"))
                        {
                            var claims = new[]
                            {
                                new Claim(ClaimTypes.NameIdentifier, context.Username, ClaimValueTypes.String, context.Options.ClaimsIssuer),
                                new Claim(ClaimTypes.Name, context.Username, ClaimValueTypes.String, context.Options.ClaimsIssuer),
                                new Claim(ClaimTypes.Role, "Admin")
                            };

                            context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));
                            context.Success();
                        }

                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization();
#endif
            services.AddRazorPages();
            services.AddFluentValidationAutoValidation();
            services.AddFluentValidationClientsideAdapters();
            services.AddValidatorsFromAssemblyContaining<Startup>();

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
                 endpoints.MapRazorPages().RequireAuthorization();
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