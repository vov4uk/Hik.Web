using System.Diagnostics.CodeAnalysis;
using HikConsole.DataAccess.Data;
using HikConsole.DataAccess.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HikConsole.DataAccess.Mappings
{
    [ExcludeFromCodeCoverage]
    public sealed class CameraMapping : IEntityTypeConfiguration<Camera>
    {
        public void Configure(EntityTypeBuilder<Camera> builder)
        {
            builder.ToTable(Tables.Camera);
            builder.HasKey(camera => camera.Id);

            builder.Property(f => f.Alias)
                .HasMaxLength(30);
            builder.Property(f => f.DestinationFolder)
                .HasMaxLength(255);
            builder.Property(f => f.IpAddress)
                .HasMaxLength(30);
            builder.Property(f => f.UserName)
                .HasMaxLength(30);
            builder.Property(f => f.LastVideoSync)
                .IsRequired(false)
                .HasColumnType("datetime2(0)");
        }
    }
}
