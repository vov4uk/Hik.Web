using Hik.DataAccess.Data;
using Hik.DataAccess.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hik.DataAccess.Mappings
{
    public class DeleteHistoryMapping : IEntityTypeConfiguration<DeleteHistory>
    {
        public void Configure(EntityTypeBuilder<DeleteHistory> builder)
        {
            builder.ToTable(Tables.DeleteHistory);
            builder.HasKey(file => file.Id);

            builder
                .HasOne(v => v.Job)
                .WithMany(job => job.DeletedFiles)
                .HasForeignKey(v => v.JobId);

            builder
                .HasOne(v => v.MediaFile)
                .WithOne(job => job.DeleteHistory)
                .HasForeignKey<DeleteHistory>(x => x.MediaFileId);
        }
    }
}
