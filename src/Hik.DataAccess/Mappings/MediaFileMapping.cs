using System.Diagnostics.CodeAnalysis;
using Hik.DataAccess.Data;
using Hik.DataAccess.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hik.DataAccess.Mappings
{
    [ExcludeFromCodeCoverage]
    public sealed class MediaFileMapping : IEntityTypeConfiguration<MediaFile>
    {
        public void Configure(EntityTypeBuilder<MediaFile> builder)
        {
            builder.ToTable(Tables.MediaFile);
            builder.HasKey(file => file.Id);
            builder.HasIndex(file => file.Date);

            builder
                .HasOne(v => v.JobTrigger)
                .WithMany(v => v.MediaFiles)
                .HasForeignKey(v => v.JobTriggerId);

            builder.Property(f => f.Date)
                .HasColumnType("datetime2(0)");

            builder.Property(f => f.Duration)
                .IsRequired(false);

            builder.Property(f => f.Name)
                .HasMaxLength(30);
        }
    }
}
