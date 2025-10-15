namespace BuildingBlocks.Saga.Abstractions;

/// <summary>
/// Base class for saga state
/// </summary>
public abstract class SagaState
{
    /// <summary>
    /// Unique identifier for this saga instance
    /// </summary>
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Current step in the saga execution
    /// </summary>
    public int CurrentStep { get; set; }
    
    /// <summary>
    /// Current status of the saga
    /// </summary>
    public SagaStatus Status { get; set; } = SagaStatus.NotStarted;
    
    /// <summary>
    /// Timestamp when saga was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Timestamp when saga was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Timestamp when saga completed (success or failure)
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// Error message if saga failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Stack trace if saga failed
    /// </summary>
    public string? ErrorStackTrace { get; set; }
    
    /// <summary>
    /// List of completed steps for tracking
    /// </summary>
    public List<string> CompletedSteps { get; set; } = [];
    
    /// <summary>
    /// List of compensated steps for rollback tracking
    /// </summary>
    public List<string> CompensatedSteps { get; set; } = [];
    
    /// <summary>
    /// Mark the saga as started
    /// </summary>
    public void MarkAsStarted()
    {
        Status = SagaStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Mark the saga as completed successfully
    /// </summary>
    public void MarkAsCompleted()
    {
        Status = SagaStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Mark the saga as failed
    /// </summary>
    public void MarkAsFailed(string error, string? stackTrace = null)
    {
        Status = SagaStatus.Failed;
        ErrorMessage = error;
        ErrorStackTrace = stackTrace;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Mark the saga as compensating (rolling back)
    /// </summary>
    public void MarkAsCompensating()
    {
        Status = SagaStatus.Compensating;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Mark the saga as compensated (rolled back)
    /// </summary>
    public void MarkAsCompensated()
    {
        Status = SagaStatus.Compensated;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Add a completed step
    /// </summary>
    public void AddCompletedStep(string stepName)
    {
        CompletedSteps.Add($"{stepName}:{DateTime.UtcNow:O}");
        CurrentStep++;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Add a compensated step
    /// </summary>
    public void AddCompensatedStep(string stepName)
    {
        CompensatedSteps.Add($"{stepName}:{DateTime.UtcNow:O}");
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Saga execution status
/// </summary>
public enum SagaStatus
{
    NotStarted,
    InProgress,
    Completed,
    Failed,
    Compensating,
    Compensated
}
