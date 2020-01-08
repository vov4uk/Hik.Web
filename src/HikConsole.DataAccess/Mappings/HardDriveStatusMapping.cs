using System.Diagnostics.CodeAnalysis;
using HikConsole.DataAccess.Data;
using HikConsole.DataAccess.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HikConsole.DataAccess.Mappings
{
    [ExcludeFromCodeCoverage]
    public sealed class HardDriveStatusMapping : IEntityTypeConfiguration<HardDriveStatus>
    {
        public void Configure(EntityTypeBuilder<HardDriveStatus> builder)
        {
            builder.ToTable(Tables.HardDriveStatus, Schemas.Dbo);
            builder.HasKey(video => video.Id);

            builder
                .HasOne(v => v.Job)
                .WithMany(job => job.HardDriveStatuses)
                .HasForeignKey(v => v.JobId);

            builder
                .HasOne(v => v.Camera)
                .WithMany(camera => camera.HardDriveStatuses)
                .HasForeignKey(v => v.CameraId);
        }
    }
}
