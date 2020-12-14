using System.Diagnostics.CodeAnalysis;
using HikConsole.DataAccess.Data;
using HikConsole.DataAccess.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HikConsole.DataAccess.Mappings
{
    [ExcludeFromCodeCoverage]
    public sealed class JobMapping : IEntityTypeConfiguration<HikJob>
    {
        public void Configure(EntityTypeBuilder<HikJob> builder)
        {
            builder.ToTable(Tables.Job);
            builder.HasKey(job => job.Id);

            builder.Property(f => f.Success)
                .HasDefaultValue("true");
            builder.Property(f => f.JobType)
                .HasMaxLength(30);
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
