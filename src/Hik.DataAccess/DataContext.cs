using Hik.DataAccess.Data;
using Hik.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Hik.DataAccess
{
    public class DataContext : DbContext
    {
        private readonly string connectionString;
        public DataContext(DbContextOptions<DataContext> options)
            : base(options) {}

        public DataContext(string connection)
            : base()
        {
            connectionString = connection;
        }

        public DbSet<HikJob> Jobs { get; set; }

        public DbSet<MediaFile> MediaFiles { get; set; }
        public DbSet<DeleteHistory> DeleteHistory { get; set; }
        public DbSet<DownloadHistory> DownloadHistory { get; set; }
        public DbSet<DownloadDuration> DownloadDuration { get; set; }

        public DbSet<DailyStatistic> DailyStatistics { get; set; }

        public DbSet<ExceptionLog> Exceptions { get; set; }

        public DbSet<JobTrigger> JobTriggers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            if (!string.IsNullOrEmpty(connectionString))
            {
                optionsBuilder.UseSqlite(connectionString, options =>
                {
                    options.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
                });
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
                .ApplyConfiguration(new DeleteHistoryMapping())
                .ApplyConfiguration(new DownloadDurationMapping());

            base.OnModelCreating(modelBuilder);
        }
    }
}
