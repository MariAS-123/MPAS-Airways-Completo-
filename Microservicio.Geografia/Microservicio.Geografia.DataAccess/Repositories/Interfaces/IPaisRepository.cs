using Microservicio.Geografia.DataAccess.Entities;

namespace Microservicio.Geografia.DataAccess.Repositories.Interfaces;

public interface IPaisRepository
{
    Task<IEnumerable<PaisEntity>> ObtenerTodosAsync(
        CancellationToken cancellationToken = default);

    Task<PaisEntity?> ObtenerPorIdAsync(
        int idPais,
        CancellationToken cancellationToken = default);

    Task<PaisEntity?> ObtenerPorCodigoIso2Async(
        string codigoIso2,
        CancellationToken cancellationToken = default);

    Task<PaisEntity?> ObtenerPorCodigoIso3Async(
        string codigoIso3,
        CancellationToken cancellationToken = default);

    Task<PaisEntity?> ObtenerPorNombreAsync(
        string nombre,
        CancellationToken cancellationToken = default);

    Task<bool> ExistePorIdAsync(
        int idPais,
        CancellationToken cancellationToken = default);

    Task<bool> ExistePorCodigoIso2Async(
        string codigoIso2,
        CancellationToken cancellationToken = default);

    Task AgregarAsync(
        PaisEntity entity,
        CancellationToken cancellationToken = default);

    void Actualizar(PaisEntity entity);
}