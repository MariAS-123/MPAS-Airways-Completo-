using Microservicio.Aeropuertos.DataManagement.Common;
using Microservicio.Aeropuertos.DataManagement.Interfaces;
using Microservicio.Aeropuertos.DataManagement.Mappers;
using Microservicio.Aeropuertos.DataManagement.Models;

using Microservicio.Aeropuertos.DataAccess.Repositories.Interfaces;

namespace Microservicio.Aeropuertos.DataManagement.Services;

public class AeropuertoDataService
    : IAeropuertoDataService
{
    private readonly IAeropuertoRepository _repo;

    private readonly IUnitOfWork _uow;

    public AeropuertoDataService(
        IAeropuertoRepository repo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<DataPagedResult<AeropuertoDataModel>> GetPagedAsync(
        AeropuertoFiltroDataModel filtro,
        CancellationToken cancellationToken = default)
    {
        filtro.PageNumber =
            filtro.PageNumber <= 0
                ? 1
                : filtro.PageNumber;

        filtro.PageSize =
            filtro.PageSize <= 0
                ? 10
                : filtro.PageSize;

        var data =
            await _repo.ObtenerTodosAsync(
                cancellationToken);

        var query =
            data.AsQueryable();

        if (!filtro.IncluirEliminados)
        {
            query =
                query.Where(x => !x.Eliminado);
        }

        if (!string.IsNullOrWhiteSpace(filtro.CodigoIata))
        {
            var codigoIata =
                filtro.CodigoIata.Trim();

            query =
                query.Where(x =>
                    x.CodigoIata.Contains(
                        codigoIata,
                        StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filtro.CodigoIcao))
        {
            var codigoIcao =
                filtro.CodigoIcao.Trim();

            query =
                query.Where(x =>
                    x.CodigoIcao != null &&
                    x.CodigoIcao.Contains(
                        codigoIcao,
                        StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Nombre))
        {
            var nombre =
                filtro.Nombre.Trim();

            query =
                query.Where(x =>
                    x.Nombre.Contains(
                        nombre,
                        StringComparison.OrdinalIgnoreCase));
        }

        if (filtro.IdCiudad.HasValue)
        {
            query =
                query.Where(x =>
                    x.IdCiudad == filtro.IdCiudad.Value);
        }

        if (filtro.IdPais.HasValue)
        {
            query =
                query.Where(x =>
                    x.IdPais == filtro.IdPais.Value);
        }

        if (!string.IsNullOrWhiteSpace(filtro.ZonaHoraria))
        {
            var zonaHoraria =
                filtro.ZonaHoraria.Trim();

            query =
                query.Where(x =>
                    x.ZonaHoraria != null &&
                    x.ZonaHoraria.Contains(
                        zonaHoraria,
                        StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Estado))
        {
            var estado =
                filtro.Estado.Trim();

            query =
                query.Where(x =>
                    x.Estado == estado);
        }

        query =
            query.OrderBy(x => x.Nombre)
                 .ThenBy(x => x.CodigoIata);

        var totalRecords =
            query.Count();

        var items =
            query.Skip(
                    (filtro.PageNumber - 1)
                    * filtro.PageSize)
                 .Take(filtro.PageSize)
                 .Select(AeropuertoDataMapper.ToDataModel)
                 .ToList();

        return new DataPagedResult<AeropuertoDataModel>
        {
            Items = items,
            PageNumber = filtro.PageNumber,
            PageSize = filtro.PageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<AeropuertoDataModel?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var entity =
            await _repo.ObtenerPorIdAsync(
                id,
                cancellationToken);

        return entity is null
            ? null
            : AeropuertoDataMapper.ToDataModel(entity);
    }

    public async Task<AeropuertoDataModel?> GetByCodigoIataAsync(
        string codigoIata,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codigoIata))
        {
            return null;
        }

        var entity =
            await _repo.ObtenerPorCodigoIataAsync(
                codigoIata.Trim().ToUpperInvariant(),
                cancellationToken);

        return entity is null
            ? null
            : AeropuertoDataMapper.ToDataModel(entity);
    }

    public async Task<IReadOnlyList<AeropuertoDataModel>> GetByPaisAsync(
        int idPais,
        CancellationToken cancellationToken = default)
    {
        var entities =
            await _repo.ObtenerPorPaisAsync(
                idPais,
                cancellationToken);

        return entities
            .Select(AeropuertoDataMapper.ToDataModel)
            .ToList();
    }

    public async Task<IReadOnlyList<AeropuertoDataModel>> GetByCiudadAsync(
        int idCiudad,
        CancellationToken cancellationToken = default)
    {
        var entities =
            await _repo.ObtenerPorCiudadAsync(
                idCiudad,
                cancellationToken);

        return entities
            .Select(AeropuertoDataMapper.ToDataModel)
            .ToList();
    }

    public async Task<AeropuertoDataModel> CreateAsync(
        AeropuertoDataModel model,
        CancellationToken cancellationToken = default)
    {
        var entity =
            AeropuertoDataMapper.ToEntity(model);

        await _repo.AgregarAsync(
            entity,
            cancellationToken);

        await _uow.SaveChangesAsync(
            cancellationToken);

        return AeropuertoDataMapper.ToDataModel(entity);
    }

    public async Task<AeropuertoDataModel?> UpdateAsync(
        AeropuertoDataModel model,
        CancellationToken cancellationToken = default)
    {
        var entity =
            await _repo.ObtenerPorIdParaEditarAsync(
                model.IdAeropuerto,
                cancellationToken);

        if (entity is null)
        {
            return null;
        }

        AeropuertoDataMapper.UpdateEntity(
            entity,
            model);

        await _uow.SaveChangesAsync(
            cancellationToken);

        return AeropuertoDataMapper.ToDataModel(entity);
    }

    public async Task<bool> DeleteAsync(
        int id,
        string modificadoPorUsuario,
        CancellationToken cancellationToken = default)
    {
        var entity =
            await _repo.ObtenerPorIdParaEditarAsync(
                id,
                cancellationToken);

        if (entity is null)
        {
            return false;
        }

        entity.Eliminado = true;

        entity.Estado = "INACTIVO";

        entity.ModificadoPorUsuario =
            modificadoPorUsuario.Trim();

        entity.FechaModificacionUtc =
            DateTime.UtcNow;

        await _uow.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}