using System.Collections;
using DoyamoyeeMondir.Application.Interfaces.Persistence;
using DoyamoyeeMondir.Domain.Common;
using DoyamoyeeMondir.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace DoyamoyeeMondir.Infrastructure.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    private readonly Hashtable _repositories = [];

    private IDbContextTransaction? _transaction;

    private bool _disposed;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IGenericRepository<TEntity> Repository<TEntity>()
        where TEntity : BaseEntity
    {
        var typeName = typeof(TEntity).Name;

        if (_repositories.ContainsKey(typeName))
        {
            return (IGenericRepository<TEntity>)
                _repositories[typeName]!;
        }

        var repository =
            new GenericRepository<TEntity>(_context);

        _repositories.Add(typeName, repository);

        return repository;
    }

    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(
            cancellationToken);
    }

    public async Task BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            return;
        }

        _transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);
    }

    public async Task CommitTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        try
        {
            await _context.SaveChangesAsync(
                cancellationToken);

            await _transaction.CommitAsync(
                cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(
                cancellationToken);

            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task RollbackTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.RollbackAsync(
            cancellationToken);

        await DisposeTransactionAsync();
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _context.Dispose();
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
        }

        await _context.DisposeAsync();

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}