using System.Diagnostics.CodeAnalysis;
using HikConsole.DataAccess.Data;
using HikConsole.DataAccess.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HikConsole.DataAccess.Mappings
{
    [ExcludeFromCodeCoverage]
    public sealed class DailyStatisticMapping : IEntityTypeConfiguration<DailyStatistic>
    {
        public void Configure(EntityTypeBuilder<DailyStatistic> builder)
        {
            builder.ToTable(Tables.DailyStatistics);
            builder.HasKey(stat => stat.Id);

            builder
                .HasOne(v => v.Camera)
                .WithMany(camera => camera.DailyStatistics)
                .HasForeignKey(v => v.CameraId);
            builder.Property(f => f.Period)
                .HasColumnType("datetime2(0)");
        }
    }
}
