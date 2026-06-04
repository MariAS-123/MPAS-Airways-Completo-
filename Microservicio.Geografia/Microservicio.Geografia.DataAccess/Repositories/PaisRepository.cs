using Microsoft.EntityFrameworkCore;
using Microservicio.Geografia.DataAccess.Context;
using Microservicio.Geografia.DataAccess.Entities;
using Microservicio.Geografia.DataAccess.Repositories.Interfaces;

namespace Microservicio.Geografia.DataAccess.Repositories;

public class PaisRepository : IPaisRepository
{
    private readonly GeografiaDbContext _context;

    public PaisRepository(GeografiaDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PaisEntity>> ObtenerTodosAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Paises
            .AsNoTracking()
            .Where(p => !p.Eliminado)
            .ToListAsync(cancellationToken);
    }

    public async Task<PaisEntity?> ObtenerPorIdAsync(
        int idPais,
        CancellationToken cancellationToken = default)
    {
        return await _context.Paises
            .FirstOrDefaultAsync(
                p => p.IdPais == idPais && !p.Eliminado,
                cancellationToken);
    }

    public async Task<PaisEntity?> ObtenerPorCodigoIso2Async(
        string codigoIso2,
        CancellationToken cancellationToken = default)
    {
        return await _context.Paises
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.CodigoIso2 == codigoIso2 && !p.Eliminado,
                cancellationToken);
    }

    public async Task<PaisEntity?> ObtenerPorNombreAsync(
        string nombre,
        CancellationToken cancellationToken = default)
    {
        return await _context.Paises
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Nombre == nombre && !p.Eliminado,
                cancellationToken);
    }

    public async Task<bool> ExistePorIdAsync(
        int idPais,
        CancellationToken cancellationToken = default)
    {
        return await _context.Paises
            .AnyAsync(
                p => p.IdPais == idPais && !p.Eliminado,
                cancellationToken);
    }

    public async Task<bool> ExistePorCodigoIso2Async(
        string codigoIso2,
        CancellationToken cancellationToken = default)
    {
        return await _context.Paises
            .AnyAsync(
                p => p.CodigoIso2 == codigoIso2 && !p.Eliminado,
                cancellationToken);
    }

    public async Task AgregarAsync(
        PaisEntity entity,
        CancellationToken cancellationToken = default)
    {
        await _context.Paises.AddAsync(entity, cancellationToken);
    }

    public async Task<PaisEntity?> ObtenerPorCodigoIso3Async(
    string codigoIso3,
    CancellationToken cancellationToken = default)
    {
        return await _context.Paises
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.CodigoIso3 == codigoIso3
                  && !p.Eliminado,
                cancellationToken);
    }

    public void Actualizar(PaisEntity entity)
    {
        _context.Paises.Update(entity);
    }
}