using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microservicio.Seguridad.DataAccess.Entities;

namespace Microservicio.Seguridad.DataAccess.Configurations
{
    public class UsuarioRolConfiguration : IEntityTypeConfiguration<UsuarioRolEntity>
    {
        public void Configure(EntityTypeBuilder<UsuarioRolEntity> builder)
        {
            builder.ToTable("usuarios_roles", "seg");

            builder.HasKey(e => e.IdUsuarioRol)
                .HasName("pk_usuarios_roles");

            builder.Property(e => e.IdUsuarioRol)
                .HasColumnName("id_usuario_rol");

            builder.Property(e => e.IdUsuario)
                .HasColumnName("id_usuario")
                .IsRequired();

            builder.Property(e => e.IdRol)
                .HasColumnName("id_rol")
                .IsRequired();

            builder.Property(e => e.EstadoUsuarioRol)
                .HasColumnName("estado_usuario_rol")
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

            builder.HasIndex(e => new { e.IdUsuario, e.IdRol })
                .IsUnique()
                .HasDatabaseName("uq_usuarios_roles_usr_rol");

            builder.HasIndex(e => e.IdUsuario)
                .HasDatabaseName("ix_usuarios_roles_usuario");

            builder.HasIndex(e => e.IdRol)
                .HasDatabaseName("ix_usuarios_roles_rol");

            // FKs internas — ambas tablas viven en BDD_Seguridad, se mantienen
            builder.HasOne(e => e.Usuario)
                .WithMany(u => u.UsuariosRoles)
                .HasForeignKey(e => e.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_usuarios_roles_usuario");

            builder.HasOne(e => e.Rol)
                .WithMany(r => r.UsuariosRoles)
                .HasForeignKey(e => e.IdRol)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_usuarios_roles_rol");
        }
    }
}