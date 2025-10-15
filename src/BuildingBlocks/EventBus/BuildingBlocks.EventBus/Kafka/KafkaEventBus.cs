using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BuildingBlocks.EventBus.Kafka;

/// <summary>
/// Kafka implementation of IEventBus for publishing integration events
/// </summary>
public sealed class KafkaEventBus(
    IProducer<string, string> producer,
    ILogger<KafkaEventBus> logger,
    string topicPrefix = "domain")
    : IEventBus, IDisposable
{
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent
    {
        var topicName = GetTopicName(@event);
        var eventJson = JsonSerializer.Serialize(@event, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        try
        {
            var message = new Message<string, string>
            {
                Key = @event.AggregateId.ToString(),
                Value = eventJson,
                Headers = new Headers
                {
                    { "event-type", System.Text.Encoding.UTF8.GetBytes(@event.GetType().Name) },
                    { "event-id", System.Text.Encoding.UTF8.GetBytes(@event.Id.ToString()) },
                    { "timestamp", System.Text.Encoding.UTF8.GetBytes(@event.OccurredOn.ToString("O")) }
                }
            };

            var result = await producer.ProduceAsync(topicName, message, cancellationToken);

            logger.LogInformation(
                "Published event {EventType} with ID {EventId} to topic {Topic} at partition {Partition}, offset {Offset}",
                @event.GetType().Name,
                @event.Id,
                topicName,
                result.Partition.Value,
                result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            logger.LogError(ex,
                "Failed to publish event {EventType} with ID {EventId} to topic {Topic}. Error: {Error}",
                @event.GetType().Name,
                @event.Id,
                topicName,
                ex.Error.Reason);
            throw;
        }
    }

    public async Task PublishBatchAsync<TEvent>(
        IEnumerable<TEvent> events,
        CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent
    {
        var tasks = events.Select(e => PublishAsync(e, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private string GetTopicName<TEvent>(TEvent @event) where TEvent : IntegrationEvent
    {
        // Extract service name from event namespace
        // e.g., Order.Domain.Events.OrderCreatedEvent -> order.events
        var eventType = @event.GetType();
        var serviceName = eventType.Namespace?.Split('.').FirstOrDefault()?.ToLowerInvariant() ?? "unknown";
        
        return $"{topicPrefix}.{serviceName}.events";
    }

    public void Dispose()
    {
        producer?.Flush(TimeSpan.FromSeconds(10));
        producer?.Dispose();
    }
}
