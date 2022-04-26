using Hik.DataAccess.Data;
using Hik.DataAccess.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hik.DataAccess.Mappings
{
    public class DownloadDurationMapping : IEntityTypeConfiguration<DownloadDuration>
    {
        public void Configure(EntityTypeBuilder<DownloadDuration> builder) 
        {
            builder.ToTable(Tables.DownloadDuration);
            builder.HasKey(file => file.Id);

            builder
                .HasOne(v => v.MediaFile)
                .WithOne(job => job.DownloadDuration)
                .HasForeignKey<DownloadDuration>(x => x.MediaFileId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(f => f.Started)
                    .IsRequired(false)
                    .HasColumnType("datetime2(0)");

            builder.Property(f => f.Duration)
                    .IsRequired(false);
        }
    }
}
