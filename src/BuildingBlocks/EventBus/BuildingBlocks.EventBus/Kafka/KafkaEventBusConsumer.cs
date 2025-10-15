using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BuildingBlocks.EventBus.Kafka;

/// <summary>
/// Background service for consuming Kafka events
/// </summary>
public sealed class KafkaEventBusConsumer<THandler>(
    IServiceProvider serviceProvider,
    ConsumerConfig config,
    string topic,
    ILogger<KafkaEventBusConsumer<THandler>> logger)
    : BackgroundService
    where THandler : IIntegrationEventHandler
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
            {
                logger.LogError("Kafka consumer error: {Error}", error.Reason);
            })
            .SetPartitionsAssignedHandler((_, partitions) =>
            {
                logger.LogInformation(
                    "Partitions assigned: {Partitions}",
                    string.Join(", ", partitions.Select(p => $"{p.Topic}[{p.Partition}]")));
            })
            .Build();

        try
        {
            consumer.Subscribe(topic);
            logger.LogInformation("Subscribed to topic: {Topic}", topic);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    
                    if (consumeResult.IsPartitionEOF)
                        continue;

                    logger.LogInformation(
                        "Received message from {Topic}[{Partition}] at offset {Offset}",
                        consumeResult.Topic,
                        consumeResult.Partition.Value,
                        consumeResult.Offset.Value);

                    await ProcessMessageAsync(consumeResult.Message.Value, stoppingToken);

                    // Commit offset after successful processing
                    consumer.Commit(consumeResult);
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Error consuming message: {Error}", ex.Error.Reason);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing message");
                    // Don't commit offset on error - message will be reprocessed
                }
            }
        }
        finally
        {
            consumer.Close();
            logger.LogInformation("Consumer closed for topic: {Topic}", topic);
        }
    }

    private async Task ProcessMessageAsync(string messageValue, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<THandler>();

        // Deserialize based on handler's event type
        // This is a simplified version - production should use proper event routing
        var eventData = JsonSerializer.Deserialize<Dictionary<string, object>>(messageValue);
        
        await handler.HandleAsync(messageValue, cancellationToken);

        logger.LogInformation("Successfully processed message");
    }
}
