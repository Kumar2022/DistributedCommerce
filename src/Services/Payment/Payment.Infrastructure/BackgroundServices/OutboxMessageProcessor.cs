using System.Text.Json;
using BuildingBlocks.EventBus.Abstractions;
using BuildingBlocks.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Payment.Application.IntegrationEvents;
using Payment.Domain.Events;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that processes outbox messages and publishes them to Kafka
/// Implements the Transactional Outbox Pattern for reliable event publishing
/// </summary>
public sealed class OutboxMessageProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxMessageProcessor> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);
    private const int MaxRetries = 3;

    public OutboxMessageProcessor(
        IServiceProvider serviceProvider,
        ILogger<OutboxMessageProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Message Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Outbox Message Processor stopped");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        // Get unprocessed messages
        var messages = await context.OutboxMessages!
            .Where(m => m.ProcessedAt == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.OccurredAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (!messages.Any())
            return;

        _logger.LogInformation(
            "Processing {Count} outbox messages",
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
                        "Published outbox message {MessageId} of type {EventType}",
                        message.Id, message.EventType);
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
                    "Failed to process outbox message {MessageId}",
                    message.Id);

                message.RetryCount++;
                message.Error = ex.Message;
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
                nameof(PaymentCreatedEvent) => ConvertPaymentCreatedEvent(message),
                nameof(PaymentSucceededEvent) => ConvertPaymentSucceededEvent(message),
                nameof(PaymentFailedEvent) => ConvertPaymentFailedEvent(message),
                nameof(PaymentRefundedEvent) => ConvertPaymentRefundedEvent(message),
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

    private IntegrationEvent? ConvertPaymentCreatedEvent(OutboxMessage message)
    {
        var domainEvent = JsonSerializer.Deserialize<PaymentCreatedEvent>(message.Payload);
        if (domainEvent == null) return null;

        return new PaymentCreatedIntegrationEvent(
            domainEvent.PaymentId,
            domainEvent.OrderId,
            domainEvent.Amount,
            domainEvent.Currency,
            domainEvent.PaymentMethod);
    }

    private IntegrationEvent? ConvertPaymentSucceededEvent(OutboxMessage message)
    {
        var domainEvent = JsonSerializer.Deserialize<PaymentSucceededEvent>(message.Payload);
        if (domainEvent == null) return null;

        return new PaymentSucceededIntegrationEvent(
            domainEvent.PaymentId,
            domainEvent.OrderId,
            domainEvent.ExternalPaymentId,
            domainEvent.Amount,
            domainEvent.Currency);
    }

    private IntegrationEvent? ConvertPaymentFailedEvent(OutboxMessage message)
    {
        var domainEvent = JsonSerializer.Deserialize<PaymentFailedEvent>(message.Payload);
        if (domainEvent == null) return null;

        return new PaymentFailedIntegrationEvent(
            domainEvent.PaymentId,
            domainEvent.OrderId,
            domainEvent.Reason,
            domainEvent.ErrorCode);
    }

    private IntegrationEvent? ConvertPaymentRefundedEvent(OutboxMessage message)
    {
        var domainEvent = JsonSerializer.Deserialize<PaymentRefundedEvent>(message.Payload);
        if (domainEvent == null) return null;

        return new PaymentRefundedIntegrationEvent(
            domainEvent.PaymentId,
            domainEvent.OrderId,
            domainEvent.RefundAmount,
            domainEvent.Currency,
            domainEvent.Reason);
    }
}
