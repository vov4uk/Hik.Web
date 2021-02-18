using System.Diagnostics.CodeAnalysis;
using Hik.DataAccess.Data;
using Hik.DataAccess.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hik.DataAccess.Mappings
{
    [ExcludeFromCodeCoverage]
    public sealed class DailyStatisticMapping : IEntityTypeConfiguration<DailyStatistic>
    {
        public void Configure(EntityTypeBuilder<DailyStatistic> builder)
        {
            builder.ToTable(Tables.DailyStatistics);
            builder.HasKey(stat => stat.Id);

            builder
                .HasOne(v => v.JobTrigger)
                .WithMany(camera => camera.DailyStatistics)
                .HasForeignKey(v => v.JobTriggerId);
            builder.Property(f => f.Period)
                .HasColumnType("datetime2(0)");
        }
    }
}
