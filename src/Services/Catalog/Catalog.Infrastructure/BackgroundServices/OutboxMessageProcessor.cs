using System.Text.Json;
using BuildingBlocks.EventBus.Abstractions;
using BuildingBlocks.Infrastructure.DeadLetterQueue;
using BuildingBlocks.Infrastructure.Outbox;
using Catalog.Application.IntegrationEvents;
using Catalog.Domain.Events;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that processes outbox messages and publishes them to Kafka
/// Implements the Transactional Outbox Pattern for reliable event publishing
/// </summary>
public sealed class OutboxMessageProcessor(
    IServiceProvider serviceProvider,
    ILogger<OutboxMessageProcessor> logger)
    : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);
    private const int MaxRetries = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Catalog Service Outbox Message Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox messages in Catalog service");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        logger.LogInformation("Catalog Service Outbox Message Processor stopped");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
        var dlqService = scope.ServiceProvider.GetRequiredService<IDeadLetterQueueService>();

        // Get unprocessed messages
        var messages = await context.OutboxMessages!
            .Where(m => m.ProcessedAt == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.OccurredAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
            return;

        logger.LogInformation(
            "Processing {Count} outbox messages from Catalog service",
            messages.Count);

        foreach (var message in messages)
        {
            try
            {
                // Convert to integration event
                var integrationEvent = ConvertToIntegrationEvent(message);

                if (integrationEvent != null)
                {
                    // Publish to Kafka
                    await eventBus.PublishAsync(integrationEvent, cancellationToken);

                    // Mark as processed
                    message.ProcessedAt = DateTime.UtcNow;
                    message.Error = null;

                    logger.LogInformation(
                        "Published outbox message {MessageId} of type {EventType} (CorrelationId: {CorrelationId})",
                        message.Id, message.EventType, message.CorrelationId);
                }
                else
                {
                    logger.LogWarning(
                        "Could not convert outbox message {MessageId} of type {EventType}",
                        message.Id, message.EventType);

                    message.RetryCount++;
                    message.Error = "Could not convert to integration event";
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to process outbox message {MessageId} (CorrelationId: {CorrelationId})",
                    message.Id, message.CorrelationId);

                message.RetryCount++;
                message.Error = ex.Message;

                // Move to DLQ after max retries
                if (message.RetryCount >= MaxRetries)
                {
                    logger.LogError(
                        "Moving message {MessageId} to DLQ after {RetryCount} retries",
                        message.Id, MaxRetries);

                    await dlqService.MoveToDeadLetterQueueAsync(
                        eventType: message.EventType,
                        payload: message.Payload,
                        failureReason: "Max retries exceeded",
                        errorDetails: ex.Message,
                        totalAttempts: message.RetryCount,
                        correlationId: message.CorrelationId,
                        originalMessageId: message.Id,
                        originalTimestamp: message.OccurredAt,
                        cancellationToken: cancellationToken);
                }
            }

            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private IntegrationEvent? ConvertToIntegrationEvent(OutboxMessage message)
    {
        try
        {
            return message.EventType switch
            {
                nameof(ProductCreatedEvent) => ConvertProductCreatedEvent(message),
                nameof(ProductUpdatedEvent) => ConvertProductUpdatedEvent(message),
                nameof(ProductPriceChangedEvent) => ConvertProductPriceChangedEvent(message),
                nameof(ProductPublishedEvent) => ConvertProductPublishedEvent(message),
                nameof(ProductUnpublishedEvent) => ConvertProductUnpublishedEvent(message),
                _ => null
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error converting outbox message {MessageId} of type {EventType}",
                message.Id, message.EventType);
            return null;
        }
    }

    private IntegrationEvent? ConvertProductCreatedEvent(OutboxMessage message)
    {
        var domainEvent = JsonSerializer.Deserialize<ProductCreatedEvent>(message.Payload);
        if (domainEvent == null) return null;

        return new ProductCreatedIntegrationEvent(
            domainEvent.ProductId,
            domainEvent.Name,
            domainEvent.Sku,
            domainEvent.CategoryId,
            0m, // TODO: Get price from Product aggregate
            "USD"); // TODO: Get currency from Product aggregate
    }

    private IntegrationEvent? ConvertProductUpdatedEvent(OutboxMessage message)
    {
        var domainEvent = JsonSerializer.Deserialize<ProductUpdatedEvent>(message.Payload);
        if (domainEvent == null) return null;

        return new ProductUpdatedIntegrationEvent(
            domainEvent.ProductId,
            domainEvent.Name,
            domainEvent.Description,
            ""); // TODO: Get brand from Product aggregate
    }

    private IntegrationEvent? ConvertProductPriceChangedEvent(OutboxMessage message)
    {
        var domainEvent = JsonSerializer.Deserialize<ProductPriceChangedEvent>(message.Payload);
        if (domainEvent == null) return null;

        return new ProductPriceChangedIntegrationEvent(
            domainEvent.ProductId,
            domainEvent.OldPrice,
            domainEvent.NewPrice,
            "USD"); // TODO: Get currency from Product aggregate
    }

    private IntegrationEvent? ConvertProductPublishedEvent(OutboxMessage message)
    {
        var domainEvent = JsonSerializer.Deserialize<ProductPublishedEvent>(message.Payload);
        if (domainEvent == null) return null;

        return new ProductPublishedIntegrationEvent(
            domainEvent.ProductId,
            domainEvent.Name,
            "", // TODO: Get SKU from Product aggregate
            message.OccurredAt);
    }

    private IntegrationEvent? ConvertProductUnpublishedEvent(OutboxMessage message)
    {
        var domainEvent = JsonSerializer.Deserialize<ProductUnpublishedEvent>(message.Payload);
        if (domainEvent == null) return null;

        return new ProductUnpublishedIntegrationEvent(
            domainEvent.ProductId,
            ""); // TODO: Get SKU from Product aggregate
    }
}
