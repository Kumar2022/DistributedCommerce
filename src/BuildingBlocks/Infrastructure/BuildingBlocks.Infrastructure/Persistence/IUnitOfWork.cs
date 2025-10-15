namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Unit of Work pattern for managing transactions across repositories
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Save all changes made in this unit of work
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Begin a database transaction
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Commit the current transaction
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rollback the current transaction
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
