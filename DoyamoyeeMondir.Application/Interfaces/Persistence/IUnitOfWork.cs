using DoyamoyeeMondir.Domain.Common;

namespace DoyamoyeeMondir.Application.Interfaces.Persistence;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IGenericRepository<TEntity> Repository<TEntity>()
        where TEntity : BaseEntity;

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(
        CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(
        CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(
        CancellationToken cancellationToken = default);
}