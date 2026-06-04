using Microsoft.EntityFrameworkCore;

using Microservicio.Aeropuertos.DataAccess.Entities;

namespace Microservicio.Aeropuertos.DataAccess.Context;

public class SistemaVuelosAeropuertosDBContext
    : DbContext
{
    public SistemaVuelosAeropuertosDBContext(
        DbContextOptions<SistemaVuelosAeropuertosDBContext> options)
        : base(options)
    {
    }

    // ============================================================
    // DBSETS
    // ============================================================
    public DbSet<AeropuertoEntity> Aeropuertos =>
        Set<AeropuertoEntity>();

    // ============================================================
    // MODEL CONFIGURATION
    // ============================================================
    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SistemaVuelosAeropuertosDBContext).Assembly);
    }
}