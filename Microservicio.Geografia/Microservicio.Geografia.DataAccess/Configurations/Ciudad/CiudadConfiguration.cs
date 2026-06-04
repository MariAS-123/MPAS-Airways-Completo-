using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microservicio.Geografia.DataAccess.Entities;

namespace Microservicio.Geografia.DataAccess.Configurations
{
    public class CiudadConfiguration : IEntityTypeConfiguration<CiudadEntity>
    {
        public void Configure(EntityTypeBuilder<CiudadEntity> builder)
        {
            builder.ToTable("ciudad", "aero");

            builder.HasKey(e => e.IdCiudad)
                .HasName("pk_ciudad");

            builder.Property(e => e.IdCiudad)
                .HasColumnName("id_ciudad");

            builder.Property(e => e.IdPais)
                .HasColumnName("id_pais")
                .IsRequired();

            builder.Property(e => e.Nombre)
                .HasColumnName("nombre")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.CodigoPostal)
                .HasColumnName("codigo_postal")
                .HasMaxLength(20);

            builder.Property(e => e.ZonaHoraria)
                .HasColumnName("zona_horaria")
                .HasMaxLength(50);

            builder.Property(e => e.Latitud)
                .HasColumnName("latitud")
                .HasColumnType("numeric(9,6)");

            builder.Property(e => e.Longitud)
                .HasColumnName("longitud")
                .HasColumnType("numeric(9,6)");

            builder.Property(e => e.Estado)
                .HasColumnName("estado")
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue("ACTIVO");

            builder.Property(e => e.Eliminado)
                .HasColumnName("eliminado")
                .HasDefaultValue(false);

            builder.Property(e => e.FechaRegistroUtc)
                .HasColumnName("fecha_registro_utc")
                .HasColumnType("timestamp")
                .IsRequired()
                .HasDefaultValueSql("(NOW() AT TIME ZONE 'UTC')");

            builder.Property(e => e.CreadoPorUsuario)
                .HasColumnName("creado_por_usuario")
                .HasMaxLength(100)
                .IsRequired()
                .HasDefaultValue("SYSTEM");

            builder.Property(e => e.ModificadoPorUsuario)
                .HasColumnName("modificado_por_usuario")
                .HasMaxLength(100);

            builder.Property(e => e.FechaModificacionUtc)
                .HasColumnName("fecha_modificacion_utc")
                .HasColumnType("timestamp");

            builder.Property(e => e.ModificacionIp)
                .HasColumnName("modificacion_ip")
                .HasMaxLength(45);

            builder.HasIndex(e => new { e.IdPais, e.Nombre })
                .IsUnique()
                .HasDatabaseName("uq_ciudad_nombre_pais");

            builder.HasOne(e => e.Pais)
                .WithMany(p => p.Ciudades)
                .HasForeignKey(e => e.IdPais)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_ciudad_pais");
        }
    }
}