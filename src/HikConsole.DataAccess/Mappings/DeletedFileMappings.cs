using System.Diagnostics.CodeAnalysis;
using HikConsole.DataAccess.Data;
using HikConsole.DataAccess.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HikConsole.DataAccess.Mappings
{
    [ExcludeFromCodeCoverage]
    public sealed class DeletedFileMappings : IEntityTypeConfiguration<DeletedFile>
    {
        public void Configure(EntityTypeBuilder<DeletedFile> builder)
        {
            builder.ToTable(Tables.DeletedFiles, Schemas.Dbo);
            builder.HasKey(video => video.Id);

            builder.Property(f => f.Extention)
                .IsRequired(false)
                .HasMaxLength(4);
            builder.Property(f => f.FilePath)
                .HasMaxLength(255);
            builder
                .HasOne(v => v.Job)
                .WithMany(job => job.DeletedFiles)
                .HasForeignKey(v => v.JobId);

            builder
                .HasOne(v => v.Camera)
                .WithMany(camera => camera.DeletedFiles)
                .HasForeignKey(v => v.CameraId);
        }
    }
}
