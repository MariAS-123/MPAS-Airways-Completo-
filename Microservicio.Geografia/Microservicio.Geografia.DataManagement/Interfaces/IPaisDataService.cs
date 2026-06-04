using Microservicio.Geografia.DataManagement.Common;
using Microservicio.Geografia.DataManagement.Models;

namespace Microservicio.Geografia.DataManagement.Interfaces;

public interface IPaisDataService
{
    Task<DataPagedResult<PaisDataModel>> GetPagedAsync(
        PaisFiltroDataModel filtro,
        CancellationToken cancellationToken = default);

    Task<PaisDataModel?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<PaisDataModel?> GetByCodigoIso2Async(
        string codigoIso2,
        CancellationToken cancellationToken = default);

    Task<PaisDataModel?> GetByCodigoIso3Async(
        string codigoIso3,
        CancellationToken cancellationToken = default);

    Task<PaisDataModel?> GetByNombreAsync(
        string nombre,
        CancellationToken cancellationToken = default);

    Task<PaisDataModel> CreateAsync(
        PaisDataModel model,
        CancellationToken cancellationToken = default);

    Task<PaisDataModel?> UpdateAsync(
        PaisDataModel model,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int id,
        string modificadoPorUsuario,
        CancellationToken cancellationToken = default);
}