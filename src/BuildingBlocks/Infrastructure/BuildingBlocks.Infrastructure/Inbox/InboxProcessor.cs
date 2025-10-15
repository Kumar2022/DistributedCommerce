using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Inbox;

/// <summary>
/// Background service that processes inbox messages
/// Ensures idempotent processing of events even if they arrive multiple times
/// </summary>
public sealed class InboxProcessor(
    IServiceProvider serviceProvider,
    ILogger<InboxProcessor> logger,
    TimeSpan? processingInterval = null)
    : BackgroundService
{
    private readonly TimeSpan _interval = processingInterval ?? TimeSpan.FromSeconds(5);
    private const int MaxAttempts = 3;
    private const int BatchSize = 100;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Inbox Processor started with interval {Interval}", _interval);

        // Small delay to let the application start up
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessInboxMessagesAsync(stoppingToken);
                await CleanupOldMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in inbox processing cycle");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        logger.LogInformation("Inbox Processor stopped");
    }

    private async Task ProcessInboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IInboxRepository>();

        // Get unprocessed messages
        var messages = await repository.GetUnprocessedAsync(BatchSize, cancellationToken);
        
        // Also get failed messages eligible for retry
        var failedMessages = await repository.GetFailedMessagesAsync(MaxAttempts, BatchSize, cancellationToken);
        
        var allMessages = messages.Concat(failedMessages).ToList();

        if (allMessages.Count == 0)
            return;

        logger.LogInformation("Processing {Count} inbox messages", allMessages.Count);

        foreach (var message in allMessages)
        {
            try
            {
                // Process the message
                await ProcessMessageAsync(scope.ServiceProvider, message, cancellationToken);
                
                // Mark as processed
                await repository.MarkAsProcessedAsync(message.Id, cancellationToken);
                
                logger.LogInformation(
                    "Successfully processed inbox message {MessageId} of type {EventType}",
                    message.Id, message.EventType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to process inbox message {MessageId} (attempt {Attempt})",
                    message.Id, message.ProcessingAttempts + 1);
                
                await repository.MarkAsFailedAsync(message.Id, ex.Message, cancellationToken);
            }
        }
    }

    private async Task ProcessMessageAsync(
        IServiceProvider serviceProvider, 
        InboxMessage message, 
        CancellationToken cancellationToken)
    {
        // This method would need to deserialize the message and route it to appropriate handler
        // For now, this is a placeholder that services will override
        logger.LogDebug(
            "Processing message {MessageId} of type {EventType}", 
            message.Id, message.EventType);
        
        await Task.CompletedTask;
        
        // NOTE: In actual implementation, each service should:
        // 1. Resolve the appropriate IIntegrationEventHandler<TEvent>
        // 2. Deserialize the payload to TEvent
        // 3. Call handler.HandleAsync(event)
    }

    private async Task CleanupOldMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IInboxRepository>();

        // Cleanup messages older than 7 days
        await repository.CleanupOldMessagesAsync(TimeSpan.FromDays(7), cancellationToken);
    }
}
