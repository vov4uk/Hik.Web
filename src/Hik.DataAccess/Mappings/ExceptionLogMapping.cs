using System.Diagnostics.CodeAnalysis;
using Hik.DataAccess.Data;
using Hik.DataAccess.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hik.DataAccess.Mappings
{
    [ExcludeFromCodeCoverage]
    public sealed class ExceptionLogMapping : IEntityTypeConfiguration<ExceptionLog>
    {
        public void Configure(EntityTypeBuilder<ExceptionLog> builder)
        {
            builder.ToTable(Tables.ExceptionLog);
            builder.HasKey(log => log.Id);

            builder
                .HasOne(v => v.Job)
                .WithOne(job => job.ExceptionLog)
                .HasForeignKey<ExceptionLog>(x => x.JobId);

            builder.Property(f => f.CallStack)
                .HasMaxLength(1000);
            builder.Property(f => f.Message)
                .HasMaxLength(255);
            builder.Property(f => f.HikErrorCode)
                .IsRequired(false);
            builder.Property(f => f.Created)
                .HasColumnType("datetime2(0)")
                .HasDefaultValueSql("datetime('now','localtime')"); 
        }
    }
}
