using Microservicio.Geografia.DataManagement.Common;
using Microservicio.Geografia.DataManagement.Models;

namespace Microservicio.Geografia.DataManagement.Interfaces;

public interface ICiudadDataService
{
    Task<DataPagedResult<CiudadDataModel>> GetPagedAsync(
        CiudadFiltroDataModel filtro,
        CancellationToken cancellationToken = default);

    Task<CiudadDataModel?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<CiudadDataModel?> GetByNombreAndPaisAsync(
        int idPais,
        string nombre,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CiudadDataModel>> GetByPaisAsync(
        int idPais,
        CancellationToken cancellationToken = default);

    Task<CiudadDataModel> CreateAsync(
        CiudadDataModel model,
        CancellationToken cancellationToken = default);

    Task<CiudadDataModel?> UpdateAsync(
        CiudadDataModel model,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int id,
        string modificadoPorUsuario,
        CancellationToken cancellationToken = default);
}