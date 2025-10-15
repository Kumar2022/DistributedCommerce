namespace BuildingBlocks.Saga.Abstractions;

/// <summary>
/// Base interface for saga orchestration
/// </summary>
public interface ISaga<TState> where TState : SagaState
{
    /// <summary>
    /// Unique identifier for the saga type
    /// </summary>
    string SagaName { get; }
    
    /// <summary>
    /// Execute the saga with the given state
    /// </summary>
    Task ExecuteAsync(TState state, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Compensate for the saga (rollback)
    /// </summary>
    Task CompensateAsync(TState state, CancellationToken cancellationToken = default);
}
