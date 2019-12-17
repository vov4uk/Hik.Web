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
            builder.ToTable(Tables.Camera, Schemas.Dbo);
            builder.HasKey(camera => camera.Id);
        }
    }
}
