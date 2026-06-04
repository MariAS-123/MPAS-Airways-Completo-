using Microservicio.Geografia.DataAccess.Context;
using Microservicio.Geografia.DataAccess.Repositories.Interfaces;
using Microservicio.Geografia.DataManagement.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microservicio.Geografia.DataManagement.Services;

public class UnitOfWork : IUnitOfWork
{
    private readonly GeografiaDbContext _context;

    private IDbContextTransaction? _currentTransaction;

    public IPaisRepository Paises { get; }

    public ICiudadRepository Ciudades { get; }

    public UnitOfWork(
        GeografiaDbContext context,
        IPaisRepository paisRepository,
        ICiudadRepository ciudadRepository)
    {
        _context = context;

        Paises = paisRepository;
        Ciudades = ciudadRepository;
    }

    public async Task BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is not null)
            return;

        _currentTransaction =
            await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
            return;

        await _currentTransaction.CommitAsync(cancellationToken);

        await _currentTransaction.DisposeAsync();

        _currentTransaction = null;
    }

    public async Task RollbackTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
            return;

        await _currentTransaction.RollbackAsync(cancellationToken);

        await _currentTransaction.DisposeAsync();

        _currentTransaction = null;
    }

    public async Task ExecuteInTransactionAsync(
        Func<Task> operation,
        CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is not null)
        {
            await operation();
            return;
        }

        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);

            await operation();

            await transaction.CommitAsync(cancellationToken);
        });
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is not null)
            return await operation();

        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);

            var result = await operation();

            await transaction.CommitAsync(cancellationToken);

            return result;
        });
    }

    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}