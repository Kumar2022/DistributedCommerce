using BuildingBlocks.Saga.Abstractions;

namespace BuildingBlocks.Saga.Storage;

/// <summary>
/// Repository for persisting saga state
/// </summary>
public interface ISagaStateRepository<TState> where TState : SagaState
{
    /// <summary>
    /// Save saga state
    /// </summary>
    Task SaveAsync(TState state, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get saga state by correlation ID
    /// </summary>
    Task<TState?> GetByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get saga state by ID
    /// </summary>
    Task<TState?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update saga state
    /// </summary>
    Task UpdateAsync(TState state, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete saga state
    /// </summary>
    Task DeleteAsync(Guid correlationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all saga states with a specific status
    /// </summary>
    Task<List<TState>> GetByStatusAsync(SagaStatus status, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all in-progress sagas older than specified time (for timeout handling)
    /// </summary>
    Task<List<TState>> GetTimedOutSagasAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
}
