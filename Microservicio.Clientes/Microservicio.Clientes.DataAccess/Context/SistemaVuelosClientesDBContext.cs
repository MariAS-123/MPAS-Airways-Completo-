using Microservicio.Clientes.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Microservicio.Clientes.DataAccess.Context
{
    public class SistemaVuelosClientesDBContext : DbContext
    {
        public SistemaVuelosClientesDBContext(
            DbContextOptions<SistemaVuelosClientesDBContext> options)
            : base(options)
        {
        }

        // ✅ Solo las tablas de BDD_Clientes
        public DbSet<ClienteEntity> Clientes => Set<ClienteEntity>();
        public DbSet<PasajeroEntity> Pasajeros => Set<PasajeroEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(SistemaVuelosClientesDBContext).Assembly);
        }
    }
}