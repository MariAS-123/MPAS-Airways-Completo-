using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Microservicio.Aeropuertos.DataAccess.Entities;

namespace Microservicio.Aeropuertos.DataAccess.Configurations;

public class AeropuertoConfiguration
    : IEntityTypeConfiguration<AeropuertoEntity>
{
    public void Configure(
        EntityTypeBuilder<AeropuertoEntity> builder)
    {
        builder.ToTable(
            "aeropuerto",
            "aero");

        // ============================================================
        // PRIMARY KEY
        // ============================================================
        builder.HasKey(e => e.IdAeropuerto)
            .HasName("pk_aeropuerto");

        builder.Property(e => e.IdAeropuerto)
            .HasColumnName("id_aeropuerto");

        // ============================================================
        // CÓDIGOS
        // ============================================================
        builder.Property(e => e.CodigoIata)
            .HasColumnName("codigo_iata")
            .HasColumnType("char(3)")
            .IsRequired();

        builder.Property(e => e.CodigoIcao)
            .HasColumnName("codigo_icao")
            .HasColumnType("char(4)");

        // ============================================================
        // NOMBRE
        // ============================================================
        builder.Property(e => e.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(150)
            .IsRequired();

        // ============================================================
        // REFERENCIAS LÓGICAS
        // ============================================================
        builder.Property(e => e.IdCiudad)
            .HasColumnName("id_ciudad");

        builder.Property(e => e.IdPais)
            .HasColumnName("id_pais")
            .IsRequired();

        // ============================================================
        // GEO
        // ============================================================
        builder.Property(e => e.ZonaHoraria)
            .HasColumnName("zona_horaria")
            .HasMaxLength(50);

        builder.Property(e => e.Latitud)
            .HasColumnName("latitud")
            .HasColumnType("numeric(9,6)");

        builder.Property(e => e.Longitud)
            .HasColumnName("longitud")
            .HasColumnType("numeric(9,6)");

        // ============================================================
        // ESTADO
        // ============================================================
        builder.Property(e => e.Estado)
            .HasColumnName("estado")
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue("ACTIVO");

        builder.Property(e => e.Eliminado)
            .HasColumnName("eliminado")
            .IsRequired()
            .HasDefaultValue(false);

        // ============================================================
        // AUDITORÍA
        // ============================================================
        builder.Property(e => e.FechaRegistroUtc)
            .HasColumnName("fecha_registro_utc")
            .HasColumnType("timestamp")
            .IsRequired()
            .HasDefaultValueSql(
                "(NOW() AT TIME ZONE 'UTC')");

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

        // ============================================================
        // ÍNDICES
        // ============================================================
        builder.HasIndex(e => e.CodigoIata)
            .IsUnique()
            .HasDatabaseName("uq_aeropuerto_iata");

        builder.HasIndex(e => e.IdPais)
            .HasDatabaseName("ix_aeropuerto_id_pais");

        builder.HasIndex(e => e.IdCiudad)
            .HasDatabaseName("ix_aeropuerto_id_ciudad");
    }
}