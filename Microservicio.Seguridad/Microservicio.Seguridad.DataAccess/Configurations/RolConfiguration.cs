using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microservicio.Seguridad.DataAccess.Entities;

namespace Microservicio.Seguridad.DataAccess.Configurations
{
    public class RolConfiguration : IEntityTypeConfiguration<RolEntity>
    {
        public void Configure(EntityTypeBuilder<RolEntity> builder)
        {
            builder.ToTable("rol", "seg");

            builder.HasKey(e => e.IdRol)
                .HasName("pk_rol");

            builder.Property(e => e.IdRol)
                .HasColumnName("id_rol");

            builder.Property(e => e.RolGuid)
                .HasColumnName("rol_guid")
                .IsRequired()
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(e => e.NombreRol)
                .HasColumnName("nombre_rol")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.DescripcionRol)
                .HasColumnName("descripcion_rol")
                .HasMaxLength(200);

            builder.Property(e => e.EstadoRol)
                .HasColumnName("estado_rol")
                .HasColumnType("char(3)")
                .IsRequired()
                .HasDefaultValue("ACT");

            builder.Property(e => e.EsEliminado)
                .HasColumnName("es_eliminado")
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(e => e.Activo)
                .HasColumnName("activo")
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(e => e.CreadoPorUsuario)
                .HasColumnName("creado_por_usuario")
                .HasMaxLength(100)
                .IsRequired()
                .HasDefaultValue("SYSTEM");

            builder.Property(e => e.FechaRegistroUtc)
                .HasColumnName("fecha_registro_utc")
                .HasColumnType("timestamp")
                .IsRequired()
                .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

            builder.Property(e => e.ModificadoPorUsuario)
                .HasColumnName("modificado_por_usuario")
                .HasMaxLength(100);

            builder.Property(e => e.FechaModificacionUtc)
                .HasColumnName("fecha_modificacion_utc")
                .HasColumnType("timestamp");

            builder.HasIndex(e => e.RolGuid)
                .IsUnique()
                .HasDatabaseName("uq_rol_guid");

            builder.HasIndex(e => e.NombreRol)
                .IsUnique()
                .HasDatabaseName("uq_rol_nombre");
        }
    }
}