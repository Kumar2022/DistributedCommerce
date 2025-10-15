using System.Text.Json;
using BuildingBlocks.EventBus.Abstractions;
using BuildingBlocks.Infrastructure.DeadLetterQueue;
using BuildingBlocks.Infrastructure.Outbox;
using Inventory.Application.IntegrationEvents;
using Inventory.Domain.Events;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that processes outbox messages and publishes them to Kafka
/// Implements the Transactional Outbox Pattern for reliable event publishing
/// </summary>
public sealed class OutboxMessageProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxMessageProcessor> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);
    private const int MaxRetries = 5;

    public OutboxMessageProcessor(
        IServiceProvider serviceProvider,
        ILogger<OutboxMessageProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Inventory Service Outbox Message Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages in Inventory service");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Inventory Service Outbox Message Processor stopped");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
        var dlqService = scope.ServiceProvider.GetRequiredService<IDeadLetterQueueService>();

        // Get unprocessed messages
        var messages = await context.OutboxMessages!
            .Where(m => m.ProcessedAt == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.OccurredAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (!messages.Any())
            return;

        _logger.LogInformation(
            "Processing {Count} outbox messages from Inventory service",
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

                    _logger.LogInformation(
                        "Published outbox message {MessageId} of type {EventType} (CorrelationId: {CorrelationId})",
                        message.Id, message.EventType, message.CorrelationId);
                }
                else
                {
                    _logger.LogWarning(
                        "Could not convert outbox message {MessageId} of type {EventType}",
                        message.Id, message.EventType);

                    message.RetryCount++;
                    message.Error = "Could not convert to integration event";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process outbox message {MessageId} (CorrelationId: {CorrelationId})",
                    message.Id, message.CorrelationId);

                message.RetryCount++;
                message.Error = ex.Message;

                // Move to DLQ after max retries
                if (message.RetryCount >= MaxRetries)
                {
                    _logger.LogError(
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
                nameof(StockReservedEvent) => ConvertStockReservedEvent(message),
                nameof(StockDeductedEvent) => ConvertStockDeductedEvent(message),
                nameof(StockReleasedEvent) => ConvertStockReleasedEvent(message),
                nameof(LowStockDetectedEvent) => ConvertLowStockDetectedEvent(message),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
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
            domainEvent.Sku,
            domainEvent.Name,
            domainEvent.InitialStock,
            message.OccurredAt);
    }

    private IntegrationEvent? ConvertStockReservedEvent(OutboxMessage message)
    {
        var domainEvent = JsonSerializer.Deserialize<StockReservedEvent>(message.Payload);
        if (domainEvent == null) return null;

        return new StockReservedIntegrationEvent(
            domainEvent.ProductId,
            domainEvent.OrderId,
            domainEvent.Quantity,
            domainEvent.AvailableQuantity,
            message.OccurredAt);
    }

    private IntegrationEvent? ConvertStockDeductedEvent(OutboxMessage message)
    {
        var domainEvent = JsonSerializer.Deserialize<StockDeductedEvent>(message.Payload);
        if (domainEvent == null) return null;

        return new StockDeductedIntegrationEvent(
            domainEvent.ProductId,
            domainEvent.OrderId,
            domainEvent.Quantity,
            domainEvent.RemainingStock,
            message.OccurredAt);
    }

    private IntegrationEvent? ConvertStockReleasedEvent(OutboxMessage message)
    {
        var domainEvent = JsonSerializer.Deserialize<StockReleasedEvent>(message.Payload);
        if (domainEvent == null) return null;

        return new StockReleasedIntegrationEvent(
            domainEvent.ProductId,
            domainEvent.OrderId,
            domainEvent.Quantity,
            domainEvent.AvailableQuantity,
            message.OccurredAt);
    }

    private IntegrationEvent? ConvertLowStockDetectedEvent(OutboxMessage message)
    {
        var domainEvent = JsonSerializer.Deserialize<LowStockDetectedEvent>(message.Payload);
        if (domainEvent == null) return null;

        return new LowStockAlertIntegrationEvent(
            domainEvent.ProductId,
            domainEvent.Sku,
            domainEvent.CurrentStock,
            domainEvent.ReorderLevel,
            domainEvent.ReorderQuantity,
            message.OccurredAt);
    }
}
