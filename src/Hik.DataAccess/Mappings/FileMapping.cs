using System.Diagnostics.CodeAnalysis;
using Hik.DataAccess.Data;
using Hik.DataAccess.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hik.DataAccess.Mappings
{
    [ExcludeFromCodeCoverage]
    public sealed class FileMapping : IEntityTypeConfiguration<MediaFile>
    {
        public void Configure(EntityTypeBuilder<MediaFile> builder)
        {
            builder.ToTable(Tables.File);
            builder.HasKey(file => file.Id);

            builder
                .HasOne(v => v.Job)
                .WithMany(job => job.Files)
                .HasForeignKey(v => v.JobId);

            builder.Property(f => f.Date)
                .HasColumnType("datetime2(0)");
            builder.Property(f => f.DownloadStarted)
                .IsRequired(false)
                .HasColumnType("datetime2(0)");
            builder.Property(f => f.DownloadDuration)
                .IsRequired(false);
            builder.Property(f => f.Duration)
                .IsRequired(false);
            builder.Property(f => f.Name)
                .HasMaxLength(30);
        }
    }
}
