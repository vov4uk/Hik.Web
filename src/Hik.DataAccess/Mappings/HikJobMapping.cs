using System.Diagnostics.CodeAnalysis;
using Hik.DataAccess.Data;
using Hik.DataAccess.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hik.DataAccess.Mappings
{
    [ExcludeFromCodeCoverage]
    public sealed class HikJobMapping : IEntityTypeConfiguration<HikJob>
    {
        public void Configure(EntityTypeBuilder<HikJob> builder)
        {
            builder.ToTable(Tables.Job);
            builder.HasKey(job => job.Id);
            builder
                .HasOne(v => v.JobTrigger)
                .WithMany(v => v.Jobs)
                .HasForeignKey(v => v.JobTriggerId);

            builder.Property(f => f.Success)
                .HasDefaultValue("true");
            builder.Property(f => f.Started)
                .IsRequired(true)
                .HasColumnType("datetime2(0)"); 
            builder.Property(f => f.Finished)
                .IsRequired(false)
                .HasColumnType("datetime2(0)");
            builder.Property(f => f.PeriodEnd)
                .IsRequired(false)
                .HasColumnType("datetime2(0)");
            builder.Property(f => f.PeriodStart)
                .IsRequired(false)
                .HasColumnType("datetime2(0)");
        }
    }
}
