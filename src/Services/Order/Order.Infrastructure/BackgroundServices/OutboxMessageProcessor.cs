using System.Text.Json;
using BuildingBlocks.EventBus.Abstractions;
using BuildingBlocks.Infrastructure.DeadLetterQueue;
using BuildingBlocks.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Order.Application.IntegrationEvents;
using Order.Domain.Events;
using Order.Infrastructure.Persistence;

namespace Order.Infrastructure.BackgroundServices;

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
        _logger.LogInformation("Order Service Outbox Message Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages in Order service");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Order Service Outbox Message Processor stopped");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderInfrastructureDbContext>();
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
            "Processing {Count} outbox messages from Order service",
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
                nameof(OrderCreatedEvent) => ConvertOrderCreatedEvent(message),
                nameof(OrderConfirmedEvent) => ConvertOrderConfirmedEvent(message),
                nameof(OrderCancelledEvent) => ConvertOrderCancelledEvent(message),
                nameof(OrderShippedEvent) => ConvertOrderShippedEvent(message),
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

    private IntegrationEvent? ConvertOrderCreatedEvent(OutboxMessage message)
    {
        var domainEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(message.Payload);
        if (domainEvent == null) return null;

        return new OrderCreatedIntegrationEvent(
            domainEvent.OrderId,
            domainEvent.CustomerId.Value,
            domainEvent.Items.Select(item => new OrderItemDto(
                item.ProductId,
                item.Quantity,
                item.UnitPrice.Amount,
                item.ProductName
            )).ToList(),
            domainEvent.TotalAmount.Amount,
            domainEvent.TotalAmount.Currency,
            domainEvent.CreatedAt);
    }

    private IntegrationEvent? ConvertOrderConfirmedEvent(OutboxMessage message)
    {
        var domainEvent = JsonSerializer.Deserialize<OrderConfirmedEvent>(message.Payload);
        if (domainEvent == null) return null;

        // Load order details from event store (Marten) to get full information
        // For now, use data from the event
        return new OrderConfirmedIntegrationEvent(
            domainEvent.OrderId,
            Guid.Empty, // TODO: Get from order aggregate
            new List<OrderItemDto>(), // TODO: Get from order aggregate
            0, // TODO: Get from order aggregate
            "USD", // TODO: Get from order aggregate
            domainEvent.ConfirmedAt);
    }

    private IntegrationEvent? ConvertOrderCancelledEvent(OutboxMessage message)
    {
        var domainEvent = JsonSerializer.Deserialize<OrderCancelledEvent>(message.Payload);
        if (domainEvent == null) return null;

        return new OrderCancelledIntegrationEvent(
            domainEvent.OrderId,
            Guid.Empty, // TODO: Get from order aggregate
            domainEvent.Reason,
            domainEvent.CancelledAt);
    }

    private IntegrationEvent? ConvertOrderShippedEvent(OutboxMessage message)
    {
        var domainEvent = JsonSerializer.Deserialize<OrderShippedEvent>(message.Payload);
        if (domainEvent == null) return null;

        return new OrderShippedIntegrationEvent(
            domainEvent.OrderId,
            Guid.Empty, // TODO: Get from order aggregate
            domainEvent.TrackingNumber,
            domainEvent.ShippedAt);
    }
}
