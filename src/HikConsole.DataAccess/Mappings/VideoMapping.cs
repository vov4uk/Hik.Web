using System.Diagnostics.CodeAnalysis;
using HikConsole.DataAccess.Data;
using HikConsole.DataAccess.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HikConsole.DataAccess.Mappings
{
    [ExcludeFromCodeCoverage]
    public sealed class VideoMapping : IEntityTypeConfiguration<Video>
    {
        public void Configure(EntityTypeBuilder<Video> builder)
        {
            builder.ToTable(Tables.Video, Schemas.Dbo);
            builder.HasKey(video => video.Id);

            builder
                .HasOne(v => v.Job)
                .WithMany(job => job.Videos)
                .HasForeignKey(v => v.JobId);

            builder
                .HasOne(v => v.Camera)
                .WithMany(camera => camera.Videos)
                .HasForeignKey(v => v.CameraId);
            builder.Property(f => f.StartTime)
                .HasColumnType("datetime2(0)");
            builder.Property(f => f.StopTime)
                .HasColumnType("datetime2(0)");
            builder.Property(f => f.DownloadStartTime)
                .HasColumnType("datetime2(0)");
            builder.Property(f => f.DownloadStopTime)
                .HasColumnType("datetime2(0)");
            builder.Property(f => f.Name)
                .HasMaxLength(30);
        }
    }
}
