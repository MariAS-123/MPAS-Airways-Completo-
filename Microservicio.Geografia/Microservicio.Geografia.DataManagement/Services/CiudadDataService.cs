using Microservicio.Geografia.DataManagement.Common;
using Microservicio.Geografia.DataManagement.Interfaces;
using Microservicio.Geografia.DataManagement.Mappers;
using Microservicio.Geografia.DataManagement.Models;

namespace Microservicio.Geografia.DataManagement.Services;

public class CiudadDataService : ICiudadDataService
{
    private readonly IUnitOfWork _uow;

    public CiudadDataService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<DataPagedResult<CiudadDataModel>> GetPagedAsync(
        CiudadFiltroDataModel filtro,
        CancellationToken cancellationToken = default)
    {
        filtro.PaginaActual =
            filtro.PaginaActual <= 0
                ? 1
                : filtro.PaginaActual;

        filtro.TamanoPagina =
            filtro.TamanoPagina <= 0
                ? 10
                : filtro.TamanoPagina;

        var data = await _uow.Ciudades
            .ObtenerTodosAsync(cancellationToken);

        var query = data.AsQueryable();

        if (!filtro.IncluirEliminados)
        {
            query = query.Where(x => !x.Eliminado);
        }

        if (filtro.IdPais.HasValue)
        {
            query = query.Where(x =>
                x.IdPais == filtro.IdPais.Value);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Nombre))
        {
            var nombre = filtro.Nombre.Trim();

            query = query.Where(x =>
                x.Nombre.Contains(
                    nombre,
                    StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filtro.CodigoPostal))
        {
            var codigoPostal = filtro.CodigoPostal.Trim();

            query = query.Where(x =>
                x.CodigoPostal != null &&
                x.CodigoPostal.Contains(
                    codigoPostal,
                    StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filtro.ZonaHoraria))
        {
            var zonaHoraria = filtro.ZonaHoraria.Trim();

            query = query.Where(x =>
                x.ZonaHoraria != null &&
                x.ZonaHoraria.Contains(
                    zonaHoraria,
                    StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Estado))
        {
            var estado = filtro.Estado.Trim().ToUpperInvariant();

            query = query.Where(x =>
                x.Estado == estado);
        }

        query = query
            .OrderBy(x => x.Nombre)
            .ThenBy(x => x.IdCiudad);

        var totalRegistros = query.Count();

        var items = query
            .Skip((filtro.PaginaActual - 1) * filtro.TamanoPagina)
            .Take(filtro.TamanoPagina)
            .Select(CiudadDataMapper.ToDataModel)
            .ToList();

        return new DataPagedResult<CiudadDataModel>
        {
            Items = items,
            PaginaActual = filtro.PaginaActual,
            TamanoPagina = filtro.TamanoPagina,
            TotalRegistros = totalRegistros
        };
    }

    public async Task<CiudadDataModel?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Ciudades
            .ObtenerPorIdAsync(id, cancellationToken);

        return entity is null
            ? null
            : CiudadDataMapper.ToDataModel(entity);
    }

    public async Task<CiudadDataModel?> GetByNombreAndPaisAsync(
        int idPais,
        string nombre,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return null;

        var entity = await _uow.Ciudades
            .ObtenerPorPaisYNombreAsync(
                idPais,
                nombre.Trim(),
                cancellationToken);

        return entity is null
            ? null
            : CiudadDataMapper.ToDataModel(entity);
    }

    public async Task<IReadOnlyList<CiudadDataModel>> GetByPaisAsync(
        int idPais,
        CancellationToken cancellationToken = default)
    {
        var data = await _uow.Ciudades
            .ObtenerPorPaisAsync(idPais, cancellationToken);

        return data
            .OrderBy(x => x.Nombre)
            .Select(CiudadDataMapper.ToDataModel)
            .ToList();
    }

    public async Task<CiudadDataModel> CreateAsync(
        CiudadDataModel model,
        CancellationToken cancellationToken = default)
    {
        var entity = CiudadDataMapper.ToEntity(model);

        entity.Estado = "ACTIVO";
        entity.Eliminado = false;
        entity.FechaRegistroUtc = DateTime.UtcNow;

        await _uow.Ciudades
            .AgregarAsync(entity, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return CiudadDataMapper.ToDataModel(entity);
    }

    public async Task<CiudadDataModel?> UpdateAsync(
        CiudadDataModel model,
        CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Ciudades
            .ObtenerPorIdAsync(
                model.IdCiudad,
                cancellationToken);

        if (entity is null || entity.Eliminado)
            return null;

        entity.FechaModificacionUtc = DateTime.UtcNow;

        CiudadDataMapper.UpdateEntity(entity, model);

        _uow.Ciudades.Actualizar(entity);

        await _uow.SaveChangesAsync(cancellationToken);

        return CiudadDataMapper.ToDataModel(entity);
    }

    public async Task<bool> DeleteAsync(
        int id,
        string modificadoPorUsuario,
        CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Ciudades
            .ObtenerPorIdAsync(id, cancellationToken);

        if (entity is null || entity.Eliminado)
            return false;

        entity.Eliminado = true;
        entity.Estado = "INACTIVO";
        entity.ModificadoPorUsuario = modificadoPorUsuario.Trim();
        entity.FechaModificacionUtc = DateTime.UtcNow;

        _uow.Ciudades.Actualizar(entity);

        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}