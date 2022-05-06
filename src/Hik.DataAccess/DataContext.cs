using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using NLog;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Hik.DataAccess
{
    [ExcludeFromCodeCoverage]
    public class DataContext : DbContext
    {
        private readonly string ConnectionString;
        public DataContext(DbContextOptions<DataContext> options)
            : base(options) { }

        public DataContext(string connection)
            : base()
        {
            ConnectionString = connection;
        }

        public DataContext(IDbConfiguration configuration)
            : base()
        {
            ConnectionString = configuration.ConnectionString;
        }

        public DbSet<HikJob> Jobs { get; set; }
        public DbSet<MediaFile> MediaFiles { get; set; }
        public DbSet<DownloadHistory> DownloadHistory { get; set; }
        public DbSet<DownloadDuration> DownloadDuration { get; set; }
        public DbSet<DailyStatistic> DailyStatistics { get; set; }
        public DbSet<ExceptionLog> Exceptions { get; set; }
        public DbSet<JobTrigger> JobTriggers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            if (!string.IsNullOrEmpty(ConnectionString))
            {
                optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

                optionsBuilder.UseSqlite(ConnectionString, options =>
                {
                    options.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
                    options.CommandTimeout(30);
                });
#if DEBUG
                optionsBuilder.EnableSensitiveDataLogging();
                optionsBuilder.LogTo(x => LogManager.GetCurrentClassLogger().Info(x), Microsoft.Extensions.Logging.LogLevel.Information);
#endif
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .ApplyConfiguration(new HikJobMapping())
                .ApplyConfiguration(new JobTriggerMapping())
                .ApplyConfiguration(new MediaFileMapping())
                .ApplyConfiguration(new ExceptionLogMapping())
                .ApplyConfiguration(new DailyStatisticMapping())
                .ApplyConfiguration(new DownloadHistoryMapping())
                .ApplyConfiguration(new DownloadDurationMapping());

            base.OnModelCreating(modelBuilder);
        }
    }
}