using Microservicio.Geografia.DataAccess.Repositories.Interfaces;

namespace Microservicio.Geografia.DataManagement.Interfaces;

public interface IUnitOfWork
{
    // Repositories
    IPaisRepository Paises { get; }

    ICiudadRepository Ciudades { get; }

    // Persistencia
    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);

    // Transacciones
    Task BeginTransactionAsync(
        CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(
        CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(
        CancellationToken cancellationToken = default);

    // Helpers transaccionales
    Task ExecuteInTransactionAsync(
        Func<Task> operation,
        CancellationToken cancellationToken = default);

    Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default);
}