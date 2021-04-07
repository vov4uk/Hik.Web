using Hik.DataAccess;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hik.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddDbContext<DataContext>(options =>
              {
                  options.UseSqlite(Configuration.GetConnectionString("HikConnectionString"), options =>
                  {
                      options.MigrationsAssembly("Hik.DataAccess.dll");
                  });
              });
        }

        public void Configure(IApplicationBuilder app, 
            IHostApplicationLifetime lifetime)
        {
            var quartz = new QuartzStartup(Configuration);

            lifetime.ApplicationStarted.Register(quartz.Start);
            lifetime.ApplicationStopping.Register(quartz.Stop);

            app.UseDeveloperExceptionPage();

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
