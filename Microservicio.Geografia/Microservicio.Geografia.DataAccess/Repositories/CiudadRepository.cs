using Microsoft.EntityFrameworkCore;
using Microservicio.Geografia.DataAccess.Context;
using Microservicio.Geografia.DataAccess.Entities;
using Microservicio.Geografia.DataAccess.Repositories.Interfaces;

namespace Microservicio.Geografia.DataAccess.Repositories;

public class CiudadRepository : ICiudadRepository
{
    private readonly GeografiaDbContext _context;

    public CiudadRepository(GeografiaDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CiudadEntity>> ObtenerTodosAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Ciudades
            .AsNoTracking()
            .Where(c => !c.Eliminado)
            .ToListAsync(cancellationToken);
    }

    public async Task<CiudadEntity?> ObtenerPorIdAsync(
        int idCiudad,
        CancellationToken cancellationToken = default)
    {
        return await _context.Ciudades
            .FirstOrDefaultAsync(
                c => c.IdCiudad == idCiudad && !c.Eliminado,
                cancellationToken);
    }

    public async Task<IEnumerable<CiudadEntity>> ObtenerPorPaisAsync(
        int idPais,
        CancellationToken cancellationToken = default)
    {
        return await _context.Ciudades
            .AsNoTracking()
            .Where(c => c.IdPais == idPais && !c.Eliminado)
            .ToListAsync(cancellationToken);
    }

    public async Task<CiudadEntity?> ObtenerPorPaisYNombreAsync(
        int idPais,
        string nombre,
        CancellationToken cancellationToken = default)
    {
        return await _context.Ciudades
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.IdPais == idPais
                  && c.Nombre == nombre
                  && !c.Eliminado,
                cancellationToken);
    }

    public async Task<bool> ExistePorIdAsync(
        int idCiudad,
        CancellationToken cancellationToken = default)
    {
        return await _context.Ciudades
            .AnyAsync(
                c => c.IdCiudad == idCiudad && !c.Eliminado,
                cancellationToken);
    }

    public async Task<bool> ExistePorPaisYNombreAsync(
        int idPais,
        string nombre,
        CancellationToken cancellationToken = default)
    {
        return await _context.Ciudades
            .AnyAsync(
                c => c.IdPais == idPais
                  && c.Nombre == nombre
                  && !c.Eliminado,
                cancellationToken);
    }

    public async Task AgregarAsync(
        CiudadEntity entity,
        CancellationToken cancellationToken = default)
    {
        await _context.Ciudades.AddAsync(entity, cancellationToken);
    }

    public void Actualizar(CiudadEntity entity)
    {
        _context.Ciudades.Update(entity);
    }
}