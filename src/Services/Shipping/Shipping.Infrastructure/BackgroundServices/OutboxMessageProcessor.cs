using System.Text.Json;
using BuildingBlocks.EventBus.Abstractions;
using BuildingBlocks.Infrastructure.DeadLetterQueue;
using BuildingBlocks.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shipping.Application.IntegrationEvents;
using Shipping.Domain.Events;
using Shipping.Infrastructure.Persistence;

namespace Shipping.Infrastructure.BackgroundServices;

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
        _logger.LogInformation("Shipping Service Outbox Message Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages in Shipping service");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Shipping Service Outbox Message Processor stopped");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ShippingDbContext>();
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
            "Processing {Count} outbox messages from Shipping service",
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
                nameof(ShipmentCreatedEvent) => ConvertShipmentCreatedEvent(message),
                nameof(ShipmentPickedUpEvent) => ConvertShipmentPickedUpEvent(message),
                nameof(ShipmentInTransitEvent) => ConvertShipmentInTransitEvent(message),
                nameof(ShipmentOutForDeliveryEvent) => ConvertShipmentOutForDeliveryEvent(message),
                nameof(ShipmentDeliveredEvent) => ConvertShipmentDeliveredEvent(message),
                nameof(ShipmentDeliveryFailedEvent) => ConvertShipmentDeliveryFailedEvent(message),
                nameof(ShipmentCancelledEvent) => ConvertShipmentCancelledEvent(message),
                nameof(ShipmentReturnedEvent) => ConvertShipmentReturnedEvent(message),
                nameof(ShipmentTrackingUpdatedEvent) => ConvertShipmentTrackingUpdatedEvent(message),
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

    private IntegrationEvent? ConvertShipmentCreatedEvent(OutboxMessage message)
    {
        var domainEvent = JsonSerializer.Deserialize<ShipmentCreatedEvent>(message.Payload);
        if (domainEvent == null) return null;

        return new ShipmentCreatedIntegrationEvent(
            domainEvent.ShipmentId,
            domainEvent.OrderId,
            domainEvent.TrackingNumber,
            domainEvent.Carrier,
            domainEvent.OccurredAt);
    }

    private IntegrationEvent? ConvertShipmentPickedUpEvent(OutboxMessage message)
    {
        var domainEvent = JsonSerializer.Deserialize<ShipmentPickedUpEvent>(message.Payload);
        if (domainEvent == null) return null;

        return new ShipmentPickedUpIntegrationEvent(
            domainEvent.ShipmentId,
            Guid.Empty, // TODO: Need to get OrderId from aggregate
            domainEvent.TrackingNumber,
            domainEvent.Carrier,
            domainEvent.PickupTime);
    }

    private IntegrationEvent? ConvertShipmentInTransitEvent(OutboxMessage message)
    {
        var domainEvent = JsonSerializer.Deserialize<ShipmentInTransitEvent>(message.Payload);
        if (domainEvent == null) return null;

        return new ShipmentInTransitIntegrationEvent(
            domainEvent.ShipmentId,
            Guid.Empty, // TODO: Need to get OrderId from aggregate
            domainEvent.TrackingNumber,
            domainEvent.CurrentLocation,
            domainEvent.EstimatedDelivery);
    }

    private IntegrationEvent? ConvertShipmentOutForDeliveryEvent(OutboxMessage message)
    {
        var domainEvent = JsonSerializer.Deserialize<ShipmentOutForDeliveryEvent>(message.Payload);
        if (domainEvent == null) return null;

        return new ShipmentOutForDeliveryIntegrationEvent(
            domainEvent.ShipmentId,
            Guid.Empty, // TODO: Need to get OrderId from aggregate
            domainEvent.TrackingNumber,
            domainEvent.EstimatedDelivery);
    }

    private IntegrationEvent? ConvertShipmentDeliveredEvent(OutboxMessage message)
    {
        var domainEvent = JsonSerializer.Deserialize<ShipmentDeliveredEvent>(message.Payload);
        if (domainEvent == null) return null;

        return new ShipmentDeliveredIntegrationEvent(
            domainEvent.ShipmentId,
            domainEvent.OrderId,
            domainEvent.TrackingNumber,
            domainEvent.DeliveryTime,
            domainEvent.RecipientName);
    }

    private IntegrationEvent? ConvertShipmentDeliveryFailedEvent(OutboxMessage message)
    {
        var domainEvent = JsonSerializer.Deserialize<ShipmentDeliveryFailedEvent>(message.Payload);
        if (domainEvent == null) return null;

        return new ShipmentDeliveryFailedIntegrationEvent(
            domainEvent.ShipmentId,
            Guid.Empty, // TODO: Need to get OrderId from aggregate
            domainEvent.TrackingNumber,
            domainEvent.Reason,
            domainEvent.AttemptNumber,
            domainEvent.NextAttemptTime);
    }

    private IntegrationEvent? ConvertShipmentCancelledEvent(OutboxMessage message)
    {
        var domainEvent = JsonSerializer.Deserialize<ShipmentCancelledEvent>(message.Payload);
        if (domainEvent == null) return null;

        return new ShipmentCancelledIntegrationEvent(
            domainEvent.ShipmentId,
            domainEvent.OrderId,
            domainEvent.Reason,
            domainEvent.CancelledAt);
    }

    private IntegrationEvent? ConvertShipmentReturnedEvent(OutboxMessage message)
    {
        var domainEvent = JsonSerializer.Deserialize<ShipmentReturnedEvent>(message.Payload);
        if (domainEvent == null) return null;

        return new ShipmentReturnedIntegrationEvent(
            domainEvent.ShipmentId,
            domainEvent.OrderId,
            domainEvent.Reason,
            domainEvent.ReturnedAt);
    }

    private IntegrationEvent? ConvertShipmentTrackingUpdatedEvent(OutboxMessage message)
    {
        var domainEvent = JsonSerializer.Deserialize<ShipmentTrackingUpdatedEvent>(message.Payload);
        if (domainEvent == null) return null;

        return new ShipmentTrackingUpdatedIntegrationEvent(
            domainEvent.ShipmentId,
            Guid.Empty, // TODO: Need to get OrderId from aggregate
            domainEvent.Location,
            domainEvent.Status,
            domainEvent.Description,
            domainEvent.Timestamp);
    }
}
