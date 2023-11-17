using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace Hik.DataAccess
{
    [ExcludeFromCodeCoverage]
    public class DataContext : DbContext
    {
        private readonly IDbConfiguration Configuration;
        public DataContext(DbContextOptions<DataContext> options)
            : base(options) { }

        public DataContext(IDbConfiguration configuration)
            : base()
        {
            this.Configuration = configuration;
        }

        public DbSet<JobTrigger> JobTriggers { get; set; }
        public DbSet<HikJob> Jobs { get; set; }
        public DbSet<MediaFile> MediaFiles { get; set; }
        public DbSet<DailyStatistic> DailyStatistics { get; set; }
        public DbSet<ExceptionLog> Exceptions { get; set; }
        public DbSet<MigrationHistory> MigrationHistory { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            if (!string.IsNullOrEmpty(this.Configuration.ConnectionString))
            {
                optionsBuilder.UseSqlite(this.Configuration.ConnectionString, options =>
                {
                    options.CommandTimeout(this.Configuration.CommandTimeout);
                });
                optionsBuilder.EnableSensitiveDataLogging();
            }
        }
    }
}