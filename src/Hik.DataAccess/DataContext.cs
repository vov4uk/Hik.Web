using Hik.DataAccess.Data;
using Hik.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using NLog;
using System.Reflection;

namespace Hik.DataAccess
{
    public class DataContext : DbContext
    {
        private readonly string ConnectionString;
        protected readonly Logger logger = LogManager.GetCurrentClassLogger();

        public DataContext(DbContextOptions<DataContext> options)
            : base(options) { }

        public DataContext(string connection)
            : base()
        {
            ConnectionString = connection;
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
                optionsBuilder.UseSqlite(ConnectionString, options =>
                {
                    options.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
                });
#if DEBUG
                optionsBuilder.EnableSensitiveDataLogging();
                optionsBuilder.LogTo(x => logger.Info(x), Microsoft.Extensions.Logging.LogLevel.Information);
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