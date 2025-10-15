namespace BuildingBlocks.Infrastructure.Inbox;

/// <summary>
/// Repository for managing inbox messages to ensure idempotent event processing
/// </summary>
public interface IInboxRepository
{
    /// <summary>
    /// Checks if a message with the given event ID has already been received
    /// </summary>
    Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a new message to the inbox
    /// Returns false if message already exists (duplicate)
    /// </summary>
    Task<bool> AddAsync(InboxMessage message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Marks a message as successfully processed
    /// </summary>
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Marks a message as failed with error details
    /// </summary>
    Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets unprocessed messages ordered by received time
    /// </summary>
    Task<List<InboxMessage>> GetUnprocessedAsync(
        int batchSize = 100, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets messages that failed processing and are eligible for retry
    /// </summary>
    Task<List<InboxMessage>> GetFailedMessagesAsync(
        int maxAttempts = 3,
        int batchSize = 100,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes old processed messages (cleanup)
    /// </summary>
    Task CleanupOldMessagesAsync(
        TimeSpan olderThan, 
        CancellationToken cancellationToken = default);
}
