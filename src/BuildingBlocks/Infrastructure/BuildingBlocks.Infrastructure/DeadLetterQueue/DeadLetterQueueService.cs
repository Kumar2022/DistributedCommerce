using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.DeadLetterQueue;

/// <summary>
/// Service for moving failed messages to DLQ and managing reprocessing
/// </summary>
public interface IDeadLetterQueueService
{
    /// <summary>
    /// Moves a message to DLQ after all retry attempts exhausted
    /// </summary>
    Task MoveToDeadLetterQueueAsync(
        string eventType,
        string payload,
        string failureReason,
        string? errorDetails,
        int totalAttempts,
        Guid? correlationId = null,
        Guid? originalMessageId = null,
        DateTime? originalTimestamp = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Attempts to reprocess a DLQ message
    /// </summary>
    Task<bool> ReprocessMessageAsync(
        Guid messageId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets DLQ statistics for monitoring
    /// </summary>
    Task<DlqStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

public sealed class DeadLetterQueueService(
    IDeadLetterQueueRepository repository,
    ILogger<DeadLetterQueueService> logger,
    string serviceName)
    : IDeadLetterQueueService
{
    public async Task MoveToDeadLetterQueueAsync(
        string eventType,
        string payload,
        string failureReason,
        string? errorDetails,
        int totalAttempts,
        Guid? correlationId = null,
        Guid? originalMessageId = null,
        DateTime? originalTimestamp = null,
        CancellationToken cancellationToken = default)
    {
        var dlqMessage = new DeadLetterMessage
        {
            EventType = eventType,
            Payload = payload,
            FailureReason = failureReason,
            ErrorDetails = errorDetails,
            TotalAttempts = totalAttempts,
            ServiceName = serviceName,
            CorrelationId = correlationId,
            OriginalMessageId = originalMessageId,
            OriginalTimestamp = originalTimestamp ?? DateTime.UtcNow
        };

        await repository.AddAsync(dlqMessage, cancellationToken);

        logger.LogError(
            "Message moved to DLQ. EventType: {EventType}, Reason: {Reason}, CorrelationId: {CorrelationId}",
            eventType, failureReason, correlationId);
    }

    public async Task<bool> ReprocessMessageAsync(
        Guid messageId, 
        CancellationToken cancellationToken = default)
    {
        var message = await repository.GetByIdAsync(messageId, cancellationToken);
        
        if (message == null)
        {
            logger.LogWarning("DLQ message {MessageId} not found", messageId);
            return false;
        }

        if (message.Reprocessed)
        {
            logger.LogWarning("DLQ message {MessageId} already reprocessed", messageId);
            return false;
        }

        try
        {
            // NOTE: Actual reprocessing logic would be implemented by each service
            // This is a placeholder that services should override
            logger.LogInformation("Reprocessing DLQ message {MessageId}", messageId);
            
            // Mark as reprocessed
            await repository.MarkAsReprocessedAsync(messageId, "Manually reprocessed", cancellationToken);
            
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reprocess DLQ message {MessageId}", messageId);
            return false;
        }
    }

    public Task<DlqStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return repository.GetStatisticsAsync(null, cancellationToken);
    }
}
