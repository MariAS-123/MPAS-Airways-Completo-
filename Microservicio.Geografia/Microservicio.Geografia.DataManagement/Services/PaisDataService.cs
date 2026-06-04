using Microservicio.Geografia.DataManagement.Common;
using Microservicio.Geografia.DataManagement.Interfaces;
using Microservicio.Geografia.DataManagement.Mappers;
using Microservicio.Geografia.DataManagement.Models;

namespace Microservicio.Geografia.DataManagement.Services;

public class PaisDataService : IPaisDataService
{
    private readonly IUnitOfWork _uow;

    public PaisDataService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<DataPagedResult<PaisDataModel>> GetPagedAsync(
        PaisFiltroDataModel filtro,
        CancellationToken cancellationToken = default)
    {
        filtro.PaginaActual =
            filtro.PaginaActual <= 0 ? 1 : filtro.PaginaActual;

        filtro.TamanoPagina =
            filtro.TamanoPagina <= 0 ? 10 : filtro.TamanoPagina;

        var data = await _uow.Paises.ObtenerTodosAsync(cancellationToken);

        var query = data.AsQueryable();

        if (!filtro.IncluirEliminados)
        {
            query = query.Where(x => !x.Eliminado);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Nombre))
        {
            var nombre = filtro.Nombre.Trim();

            query = query.Where(x =>
                x.Nombre.Contains(nombre, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filtro.CodigoIso2))
        {
            var iso2 = filtro.CodigoIso2.Trim().ToUpperInvariant();

            query = query.Where(x =>
                x.CodigoIso2 == iso2);
        }

        if (!string.IsNullOrWhiteSpace(filtro.CodigoIso3))
        {
            var iso3 = filtro.CodigoIso3.Trim().ToUpperInvariant();

            query = query.Where(x =>
                x.CodigoIso3 == iso3);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Continente))
        {
            var continente = filtro.Continente.Trim();

            query = query.Where(x =>
                x.Continente != null &&
                x.Continente.Contains(
                    continente,
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
            .ThenBy(x => x.IdPais);

        var total = query.Count();

        var items = query
            .Skip((filtro.PaginaActual - 1) * filtro.TamanoPagina)
            .Take(filtro.TamanoPagina)
            .Select(PaisDataMapper.ToDataModel)
            .ToList();

        return new DataPagedResult<PaisDataModel>
        {
            Items = items,
            PaginaActual = filtro.PaginaActual,
            TamanoPagina = filtro.TamanoPagina,
            TotalRegistros = total
        };
    }

    public async Task<PaisDataModel?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Paises
            .ObtenerPorIdAsync(id, cancellationToken);

        return entity is null
            ? null
            : PaisDataMapper.ToDataModel(entity);
    }

    public async Task<PaisDataModel?> GetByCodigoIso2Async(
        string codigoIso2,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codigoIso2))
            return null;

        var entity = await _uow.Paises
            .ObtenerPorCodigoIso2Async(
                codigoIso2.Trim().ToUpperInvariant(),
                cancellationToken);

        return entity is null
            ? null
            : PaisDataMapper.ToDataModel(entity);
    }

    public async Task<PaisDataModel?> GetByCodigoIso3Async(
        string codigoIso3,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codigoIso3))
            return null;

        var entity = await _uow.Paises
            .ObtenerPorCodigoIso3Async(
                codigoIso3.Trim().ToUpperInvariant(),
                cancellationToken);

        return entity is null
            ? null
            : PaisDataMapper.ToDataModel(entity);
    }

    public async Task<PaisDataModel?> GetByNombreAsync(
        string nombre,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return null;

        var entity = await _uow.Paises
            .ObtenerPorNombreAsync(
                nombre.Trim(),
                cancellationToken);

        return entity is null
            ? null
            : PaisDataMapper.ToDataModel(entity);
    }

    public async Task<PaisDataModel> CreateAsync(
        PaisDataModel model,
        CancellationToken cancellationToken = default)
    {
        var entity = PaisDataMapper.ToEntity(model);

        entity.Estado = "ACTIVO";
        entity.Eliminado = false;

        await _uow.Paises
            .AgregarAsync(entity, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return PaisDataMapper.ToDataModel(entity);
    }

    public async Task<PaisDataModel?> UpdateAsync(
        PaisDataModel model,
        CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Paises
            .ObtenerPorIdAsync(model.IdPais, cancellationToken);

        if (entity is null || entity.Eliminado)
            return null;

        PaisDataMapper.UpdateEntity(entity, model);

        _uow.Paises.Actualizar(entity);

        await _uow.SaveChangesAsync(cancellationToken);

        return PaisDataMapper.ToDataModel(entity);
    }

    public async Task<bool> DeleteAsync(
        int id,
        string modificadoPorUsuario,
        CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Paises
            .ObtenerPorIdAsync(id, cancellationToken);

        if (entity is null || entity.Eliminado)
        {
            return false;
        }

        entity.Eliminado = true;

        entity.Estado = "INACTIVO";

        _uow.Paises.Actualizar(entity);

        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}