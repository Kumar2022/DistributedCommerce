namespace BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Repository for managing outbox messages for the Transactional Outbox Pattern
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Adds a new message to the outbox within the current transaction
    /// </summary>
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets unprocessed messages that are ready to be published
    /// </summary>
    Task<List<OutboxMessage>> GetUnprocessedAsync(
        int batchSize = 100, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets failed messages that are eligible for retry
    /// </summary>
    Task<List<OutboxMessage>> GetFailedMessagesAsync(
        int maxRetries = 3,
        int batchSize = 100,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Marks a message as successfully processed
    /// </summary>
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Marks a message as failed and increments retry count
    /// </summary>
    Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes old processed messages (cleanup)
    /// </summary>
    Task CleanupOldMessagesAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);
}
