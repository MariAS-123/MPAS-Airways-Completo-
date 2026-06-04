using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microservicio.Seguridad.DataAccess.Entities;

namespace Microservicio.Seguridad.DataAccess.Configurations
{
    public class UsuarioAppConfiguration : IEntityTypeConfiguration<UsuarioAppEntity>
    {
        public void Configure(EntityTypeBuilder<UsuarioAppEntity> builder)
        {
            builder.ToTable("usuario_app", "seg");

            builder.HasKey(e => e.IdUsuario)
                .HasName("pk_usuario_app");

            builder.Property(e => e.IdUsuario)
                .HasColumnName("id_usuario");

            builder.Property(e => e.UsuarioGuid)
                .HasColumnName("usuario_guid")
                .IsRequired()
                .HasDefaultValueSql("gen_random_uuid()");

            // INT simple — referencia lógica al MS Clientes, sin FK física
            builder.Property(e => e.IdCliente)
                .HasColumnName("id_cliente");

            builder.Property(e => e.Username)
                .HasColumnName("username")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.Correo)
                .HasColumnName("correo")
                .HasMaxLength(120)
                .IsRequired();

            builder.Property(e => e.PasswordHash)
                .HasColumnName("password_hash")
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(e => e.PasswordSalt)
                .HasColumnName("password_salt")
                .HasMaxLength(250)
                .IsRequired();

            builder.Property(e => e.FechaUltimoLogin)
                .HasColumnName("fecha_ultimo_login")
                .HasColumnType("timestamp");

            builder.Property(e => e.EstadoUsuario)
                .HasColumnName("estado_usuario")
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

            builder.Property(e => e.ModificacionIp)
                .HasColumnName("modificacion_ip")
                .HasMaxLength(45);

            builder.HasIndex(e => e.UsuarioGuid)
                .IsUnique()
                .HasDatabaseName("uq_usuario_app_guid");

            builder.HasIndex(e => e.Username)
                .IsUnique()
                .HasDatabaseName("uq_usuario_app_username");

            builder.HasIndex(e => e.Correo)
                .IsUnique()
                .HasDatabaseName("uq_usuario_app_correo");

            builder.HasIndex(e => e.IdCliente)
                .HasDatabaseName("ix_usuario_app_id_cliente");

            // FK hacia ClienteEntity ELIMINADA — IdCliente es referencia
            // lógica al MS Clientes, validada por HTTP, no por EF Core
        }
    }
}