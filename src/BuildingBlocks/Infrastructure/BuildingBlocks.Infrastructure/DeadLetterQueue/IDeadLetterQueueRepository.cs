namespace BuildingBlocks.Infrastructure.DeadLetterQueue;

/// <summary>
/// Repository for managing dead letter queue messages
/// Stores permanently failed messages for manual intervention
/// </summary>
public interface IDeadLetterQueueRepository
{
    /// <summary>
    /// Adds a failed message to the DLQ
    /// </summary>
    Task AddAsync(DeadLetterMessage message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all DLQ messages, optionally filtered by service or date range
    /// </summary>
    Task<List<DeadLetterMessage>> GetAsync(
        string? serviceName = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        bool includeReprocessed = false,
        int pageSize = 100,
        int pageNumber = 1,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a specific DLQ message by ID
    /// </summary>
    Task<DeadLetterMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Marks a message as reprocessed
    /// </summary>
    Task MarkAsReprocessedAsync(
        Guid id, 
        string? notes = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds operator notes to a DLQ message
    /// </summary>
    Task AddNotesAsync(
        Guid id, 
        string notes, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets statistics about DLQ messages
    /// </summary>
    Task<DlqStatistics> GetStatisticsAsync(
        DateTime? fromDate = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes old reprocessed messages (cleanup)
    /// </summary>
    Task CleanupReprocessedAsync(
        TimeSpan olderThan, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics about dead letter queue
/// </summary>
public sealed class DlqStatistics
{
    public int TotalMessages { get; init; }
    public int PendingMessages { get; init; }
    public int ReprocessedMessages { get; init; }
    public Dictionary<string, int> MessagesByService { get; init; } = new();
    public Dictionary<string, int> MessagesByEventType { get; init; } = new();
    public DateTime? OldestMessageDate { get; init; }
    public DateTime? NewestMessageDate { get; init; }
}
