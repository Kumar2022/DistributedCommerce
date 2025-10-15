namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Generic repository interface for data access
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TId">The entity identifier type</typeparam>
public interface IRepository<TEntity, in TId> 
    where TEntity : Entity<TId>
    where TId : notnull
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    
    void Update(TEntity entity);
    
    void Delete(TEntity entity);
    
    Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);
}
