using System.Diagnostics.CodeAnalysis;
using Hik.DataAccess.Data;
using Hik.DataAccess.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hik.DataAccess.Mappings
{
    [ExcludeFromCodeCoverage]
    public sealed class JobTriggerMapping : IEntityTypeConfiguration<JobTrigger>
    {
        public void Configure(EntityTypeBuilder<JobTrigger> builder)
        {
            builder.ToTable(Tables.JobTrigger);
            builder.HasKey(trigger => trigger.Id);

            builder.Property(f => f.ShowInSearch)
                .IsRequired(true)
                .HasDefaultValue(true);
            builder.Property(f => f.TriggerKey)
                .HasMaxLength(30);
            builder.Property(f => f.Group)
                .HasMaxLength(30);
            builder.Property(f => f.LastSync)
                .IsRequired(false)
                .HasColumnType("datetime2(0)");
        }
    }
}
