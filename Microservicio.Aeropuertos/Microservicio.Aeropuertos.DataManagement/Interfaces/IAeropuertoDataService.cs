using Microservicio.Aeropuertos.DataManagement.Common;
using Microservicio.Aeropuertos.DataManagement.Models;

namespace Microservicio.Aeropuertos.DataManagement.Interfaces;

public interface IAeropuertoDataService
{
    Task<DataPagedResult<AeropuertoDataModel>> GetPagedAsync(
        AeropuertoFiltroDataModel filtro,
        CancellationToken cancellationToken = default);

    Task<AeropuertoDataModel?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<AeropuertoDataModel?> GetByCodigoIataAsync(
        string codigoIata,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AeropuertoDataModel>> GetByPaisAsync(
        int idPais,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AeropuertoDataModel>> GetByCiudadAsync(
        int idCiudad,
        CancellationToken cancellationToken = default);

    Task<AeropuertoDataModel> CreateAsync(
        AeropuertoDataModel model,
        CancellationToken cancellationToken = default);

    Task<AeropuertoDataModel?> UpdateAsync(
        AeropuertoDataModel model,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int id,
        string modificadoPorUsuario,
        CancellationToken cancellationToken = default);
}