using System.Diagnostics.CodeAnalysis;
using Hik.DataAccess.Data;
using Hik.DataAccess.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hik.DataAccess.Mappings
{
    [ExcludeFromCodeCoverage]
    public sealed class PhotoMapping : IEntityTypeConfiguration<Photo>
    {
        public void Configure(EntityTypeBuilder<Photo> builder)
        {
            builder.ToTable(Tables.Photo);
            builder.HasKey(video => video.Id);

            builder
                .HasOne(v => v.Job)
                .WithMany(job => job.Photos)
                .HasForeignKey(v => v.JobId);

            builder
                .HasOne(v => v.Camera)
                .WithMany(camera => camera.Photos)
                .HasForeignKey(v => v.CameraId);
            builder.Property(f => f.DateTaken)
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
