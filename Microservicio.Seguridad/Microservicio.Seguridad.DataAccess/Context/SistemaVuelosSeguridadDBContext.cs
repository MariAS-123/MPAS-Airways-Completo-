using Microservicio.Seguridad.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Microservicio.Seguridad.DataAccess.Context
{
    public class SistemaVuelosSeguridadDBContext : DbContext
    {
        public SistemaVuelosSeguridadDBContext(
            DbContextOptions<SistemaVuelosSeguridadDBContext> options)
            : base(options)
        {
        }

        // Solo las 3 tablas de BDD_Seguridad
        public DbSet<RolEntity> Roles => Set<RolEntity>();
        public DbSet<UsuarioAppEntity> Usuarios => Set<UsuarioAppEntity>();
        public DbSet<UsuarioRolEntity> UsuariosRoles => Set<UsuarioRolEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Aplica solo las 3 configuraciones de este ensamblado:
            // RolConfiguration, UsuarioAppConfiguration, UsuarioRolConfiguration
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(SistemaVuelosSeguridadDBContext).Assembly);
        }
    }
}