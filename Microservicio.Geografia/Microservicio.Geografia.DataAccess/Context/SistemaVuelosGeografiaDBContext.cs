using Microsoft.EntityFrameworkCore;
using Microservicio.Geografia.DataAccess.Configurations;
using Microservicio.Geografia.DataAccess.Entities;

namespace Microservicio.Geografia.DataAccess.Context
{
    public class GeografiaDbContext : DbContext
    {
        public GeografiaDbContext(DbContextOptions<GeografiaDbContext> options)
            : base(options)
        {
        }

        // DbSets SOLO del MS Geografía
        public DbSet<PaisEntity> Paises => Set<PaisEntity>();

        public DbSet<CiudadEntity> Ciudades => Set<CiudadEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurations
            modelBuilder.ApplyConfiguration(new PaisConfiguration());
            modelBuilder.ApplyConfiguration(new CiudadConfiguration());
        }
    }
}