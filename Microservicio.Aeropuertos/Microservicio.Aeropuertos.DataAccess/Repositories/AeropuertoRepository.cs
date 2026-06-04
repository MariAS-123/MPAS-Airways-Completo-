using Microsoft.EntityFrameworkCore;

using Microservicio.Aeropuertos.DataAccess.Context;
using Microservicio.Aeropuertos.DataAccess.Entities;
using Microservicio.Aeropuertos.DataAccess.Repositories.Interfaces;

namespace Microservicio.Aeropuertos.DataAccess.Repositories;

public class AeropuertoRepository
    : IAeropuertoRepository
{
    private readonly SistemaVuelosAeropuertosDBContext _context;

    public AeropuertoRepository(
        SistemaVuelosAeropuertosDBContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AeropuertoEntity>> ObtenerTodosAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Aeropuertos
            .AsNoTracking()
            .Where(a => !a.Eliminado)
            .ToListAsync(cancellationToken);
    }

    public async Task<AeropuertoEntity?> ObtenerPorIdAsync(
        int idAeropuerto,
        CancellationToken cancellationToken = default)
    {
        return await _context.Aeropuertos
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.IdAeropuerto == idAeropuerto
                  && !a.Eliminado,
                cancellationToken);
    }

    public async Task<AeropuertoEntity?> ObtenerPorIdParaEditarAsync(
        int idAeropuerto,
        CancellationToken cancellationToken = default)
    {
        return await _context.Aeropuertos
            .FirstOrDefaultAsync(
                a => a.IdAeropuerto == idAeropuerto
                  && !a.Eliminado,
                cancellationToken);
    }

    public async Task<AeropuertoEntity?> ObtenerPorCodigoIataAsync(
        string codigoIata,
        CancellationToken cancellationToken = default)
    {
        return await _context.Aeropuertos
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.CodigoIata == codigoIata
                  && !a.Eliminado,
                cancellationToken);
    }

    public async Task<IEnumerable<AeropuertoEntity>> ObtenerPorPaisAsync(
        int idPais,
        CancellationToken cancellationToken = default)
    {
        return await _context.Aeropuertos
            .AsNoTracking()
            .Where(a =>
                a.IdPais == idPais &&
                !a.Eliminado)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AeropuertoEntity>> ObtenerPorCiudadAsync(
        int idCiudad,
        CancellationToken cancellationToken = default)
    {
        return await _context.Aeropuertos
            .AsNoTracking()
            .Where(a =>
                a.IdCiudad == idCiudad &&
                !a.Eliminado)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistePorIdAsync(
        int idAeropuerto,
        CancellationToken cancellationToken = default)
    {
        return await _context.Aeropuertos
            .AnyAsync(
                a => a.IdAeropuerto == idAeropuerto
                  && !a.Eliminado,
                cancellationToken);
    }

    public async Task<bool> ExistePorCodigoIataAsync(
        string codigoIata,
        CancellationToken cancellationToken = default)
    {
        return await _context.Aeropuertos
            .AnyAsync(
                a => a.CodigoIata == codigoIata
                  && !a.Eliminado,
                cancellationToken);
    }

    public async Task AgregarAsync(
        AeropuertoEntity entity,
        CancellationToken cancellationToken = default)
    {
        await _context.Aeropuertos
            .AddAsync(entity, cancellationToken);
    }

    public void Actualizar(
        AeropuertoEntity entity)
    {
        _context.Aeropuertos.Update(entity);
    }
}