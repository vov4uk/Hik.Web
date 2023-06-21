using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace Hik.DataAccess
{
    [ExcludeFromCodeCoverage]
    public class DataContext : DbContext
    {
        private readonly string ConnectionString;
        public DataContext(DbContextOptions<DataContext> options)
            : base(options) { }

        public DataContext(IDbConfiguration configuration)
            : base()
        {
            ConnectionString = configuration.ConnectionString;
        }

        public DbSet<JobTrigger> JobTriggers { get; set; }
        public DbSet<HikJob> Jobs { get; set; }
        public DbSet<MediaFile> MediaFiles { get; set; }
        public DbSet<DownloadHistory> DownloadHistory { get; set; }
        public DbSet<DownloadDuration> DownloadDuration { get; set; }
        public DbSet<DailyStatistic> DailyStatistics { get; set; }
        public DbSet<ExceptionLog> Exceptions { get; set; }
        public DbSet<MigrationHistory> MigrationHistory { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            if (!string.IsNullOrEmpty(ConnectionString))
            {
                optionsBuilder.UseSqlite(ConnectionString, options =>
                {
                    options.CommandTimeout(30);
                });
#if DEBUG
                optionsBuilder.EnableSensitiveDataLogging();
#endif
            }
        }
    }
}