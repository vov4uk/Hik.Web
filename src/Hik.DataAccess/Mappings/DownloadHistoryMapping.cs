using Hik.DataAccess.Data;
using Hik.DataAccess.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hik.DataAccess.Mappings
{
    public class DownloadHistoryMapping : IEntityTypeConfiguration<DownloadHistory>
    {
        public void Configure(EntityTypeBuilder<DownloadHistory> builder)
        {
            builder.ToTable(Tables.DownloadHistory);
            builder.HasKey(file => file.Id);

            builder
                .HasOne(v => v.Job)
                .WithMany(job => job.DownloadedFiles)
                .HasForeignKey(v => v.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasOne(v => v.MediaFile)
                .WithOne(job => job.DownloadHistory)
                .HasForeignKey<DownloadHistory>(x => x.MediaFileId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
