using BuildingBlocks.EventBus.Abstractions;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BuildingBlocks.EventBus.Dispatcher;

/// <summary>
/// Generic Kafka consumer that dispatches to event handlers based on EventType
/// FAANG-scale pattern: One consumer per topic per service + dispatcher routing
/// Preserves ordering per partition while avoiding consumer explosion
/// </summary>
public class DispatchingKafkaConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IEventDispatcher _dispatcher;
    private readonly ILogger<DispatchingKafkaConsumer> _logger;
    private readonly string _topic;
    private readonly string _consumerGroup;

    public DispatchingKafkaConsumer(
        string bootstrapServers,
        string consumerGroup,
        string topic,
        IEventDispatcher dispatcher,
        ILogger<DispatchingKafkaConsumer> logger)
    {
        _topic = topic ?? throw new ArgumentNullException(nameof(topic));
        _consumerGroup = consumerGroup ?? throw new ArgumentNullException(nameof(consumerGroup));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = consumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false, // Manual commit for safety
            EnablePartitionEof = false,
            AllowAutoCreateTopics = true,
            // Consumer session timeout
            SessionTimeoutMs = 45000,
            MaxPollIntervalMs = 300000,
        };

        _consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka consumer error: {Reason}", e.Reason))
            .Build();

        _logger.LogInformation(
            "Created Kafka consumer for topic: {Topic}, consumer group: {ConsumerGroup}",
            _topic,
            _consumerGroup);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_topic);

        _logger.LogInformation(
            "Kafka consumer started for topic: {Topic}, consumer group: {ConsumerGroup}",
            _topic,
            _consumerGroup);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);

                    if (consumeResult?.Message == null)
                        continue;

                    _logger.LogDebug(
                        "Received message from topic {Topic}, partition {Partition}, offset {Offset}",
                        consumeResult.Topic,
                        consumeResult.Partition.Value,
                        consumeResult.Offset.Value);

                    await ProcessMessageAsync(consumeResult, stoppingToken);

                    // Commit offset after successful processing
                    _consumer.Commit(consumeResult);

                    _logger.LogDebug(
                        "Committed offset {Offset} for topic {Topic}, partition {Partition}",
                        consumeResult.Offset.Value,
                        consumeResult.Topic,
                        consumeResult.Partition.Value);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message from Kafka");
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in Kafka consumer loop");
                    // Don't commit on error - message will be reprocessed
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        finally
        {
            _consumer.Close();
            _logger.LogInformation("Kafka consumer stopped for topic: {Topic}", _topic);
        }
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> consumeResult, CancellationToken cancellationToken)
    {
        try
        {
            // Extract EventType from message
            var eventType = ExtractEventType(consumeResult.Message.Value);

            if (string.IsNullOrEmpty(eventType))
            {
                _logger.LogWarning("Message does not contain EventType field. Skipping.");
                return;
            }

            _logger.LogInformation(
                "Processing event type: {EventType} from topic: {Topic}",
                eventType,
                _topic);

            // Dispatch to registered handler
            await _dispatcher.DispatchAsync(eventType, consumeResult.Message.Value, cancellationToken);

            _logger.LogInformation(
                "Successfully processed event type: {EventType}",
                eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing message from topic {Topic}, partition {Partition}, offset {Offset}",
                consumeResult.Topic,
                consumeResult.Partition.Value,
                consumeResult.Offset.Value);

            // Re-throw to prevent commit and trigger retry
            throw;
        }
    }

    private string? ExtractEventType(string messageValue)
    {
        try
        {
            using var doc = JsonDocument.Parse(messageValue);
            if (doc.RootElement.TryGetProperty("EventType", out var eventTypeProp))
            {
                return eventTypeProp.GetString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting EventType from message");
        }

        return null;
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// Extension methods for registering dispatching Kafka consumer
/// </summary>
public static class DispatchingKafkaConsumerExtensions
{
    /// <summary>
    /// Register a single Kafka consumer for a topic with event dispatcher
    /// Handlers are registered separately via IEventDispatcher
    /// </summary>
    public static IServiceCollection AddDispatchingKafkaConsumer(
        this IServiceCollection services,
        string bootstrapServers,
        string consumerGroup,
        string topic)
    {
        services.AddHostedService(sp =>
        {
            var dispatcher = sp.GetRequiredService<IEventDispatcher>();
            var logger = sp.GetRequiredService<ILogger<DispatchingKafkaConsumer>>();
            
            return new DispatchingKafkaConsumer(
                bootstrapServers,
                consumerGroup,
                topic,
                dispatcher,
                logger);
        });

        return services;
    }
}
