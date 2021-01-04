using Hik.DataAccess.Data;
using Hik.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Hik.DataAccess
{
    public class DataContext : DbContext
    {
        private readonly string ConnectionString;
        public DataContext(DbContextOptions<DataContext> options)
            : base(options) {}

        public DataContext(string connection)
            : base()
        {
            ConnectionString = connection;
        }

        public DbSet<HikJob> Jobs { get; set; }
        public DbSet<Camera> Cameras { get; set; }
        public DbSet<Video> Videos { get; set; }
        public DbSet<Photo> Photos { get; set; }
        public DbSet<DeletedFile> DeletedFiles { get; set; }

        public DbSet<DailyStatistic> DailyStatistics { get; set; }

        public DbSet<ExceptionLog> Exceptions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            if (!string.IsNullOrEmpty(ConnectionString))
            {
                optionsBuilder.UseSqlite(ConnectionString, options =>
                {
                    options.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
                });
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new CameraMapping())
                .ApplyConfiguration(new JobMapping())
                .ApplyConfiguration(new VideoMapping())
                .ApplyConfiguration(new PhotoMapping())
                .ApplyConfiguration(new DeletedFileMappings())
                .ApplyConfiguration(new ExceptionLogMapping())
                .ApplyConfiguration(new DailyStatisticMapping());

            base.OnModelCreating(modelBuilder);
        }
    }
}
