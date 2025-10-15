namespace BuildingBlocks.Saga.Abstractions;

/// <summary>
/// Represents a single step in a saga
/// </summary>
public interface ISagaStep<in TState> where TState : SagaState
{
    /// <summary>
    /// Name of the step
    /// </summary>
    string StepName { get; }
    
    /// <summary>
    /// Execute this step
    /// </summary>
    Task<SagaStepResult> ExecuteAsync(TState state, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Compensate (rollback) this step
    /// </summary>
    Task CompensateAsync(TState state, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a saga step execution
/// </summary>
public class SagaStepResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public Exception? Exception { get; init; }
    
    public static SagaStepResult Success() => new() { IsSuccess = true };
    public static SagaStepResult Failure(string error, Exception? exception = null) => 
        new() { IsSuccess = false, ErrorMessage = error, Exception = exception };
}
