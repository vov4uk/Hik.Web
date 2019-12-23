using HikConsole.DataAccess.Data;
using HikConsole.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;

namespace HikConsole.DataAccess
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
        public DbSet<HardDriveStatus> HDStatus { get; set; }
        public DbSet<DeletedFile> DeletedFiles { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            if (!string.IsNullOrEmpty(ConnectionString))
            {
                optionsBuilder.UseSqlServer(ConnectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .ApplyConfiguration(new VideoMapping())
                .ApplyConfiguration(new PhotoMapping())
                .ApplyConfiguration(new JobMapping())
                .ApplyConfiguration(new DeletedFileMappings())
                .ApplyConfiguration(new CameraMapping())
                .ApplyConfiguration(new HardDriveStatusMapping());

            base.OnModelCreating(modelBuilder);
        }
    }
}
