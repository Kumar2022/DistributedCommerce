using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.EventBus.Kafka;

/// <summary>
/// Extension methods for configuring Kafka EventBus
/// </summary>
public static class KafkaEventBusExtensions
{
    public static IServiceCollection AddKafkaEventBus(
        this IServiceCollection services,
        string bootstrapServers,
        string clientId = "distributed-commerce")
    {
        // Configure Kafka Producer
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            ClientId = clientId,
            Acks = Acks.All, // Wait for all replicas
            EnableIdempotence = true, // Exactly-once semantics
            MaxInFlight = 5,
            MessageSendMaxRetries = int.MaxValue,
            CompressionType = CompressionType.Snappy,
            LingerMs = 10, // Batch messages for 10ms
            BatchSize = 16384, // 16KB batch size
            
            // Partitioning
            Partitioner = Partitioner.ConsistentRandom,
            
            // Timeouts
            MessageTimeoutMs = 300000, // 5 minutes
            RequestTimeoutMs = 30000, // 30 seconds
        };

        // Register producer as singleton
        services.AddSingleton<IProducer<string, string>>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<KafkaEventBus>>();
            
            var producer = new ProducerBuilder<string, string>(producerConfig)
                .SetErrorHandler((_, error) =>
                {
                    logger.LogError("Kafka producer error: {Error}", error.Reason);
                })
                .SetLogHandler((_, logMessage) =>
                {
                    var level = logMessage.Level switch
                    {
                        SyslogLevel.Emergency or SyslogLevel.Alert or SyslogLevel.Critical or SyslogLevel.Error => LogLevel.Error,
                        SyslogLevel.Warning => LogLevel.Warning,
                        SyslogLevel.Notice or SyslogLevel.Info => LogLevel.Information,
                        _ => LogLevel.Debug
                    };
                    
                    logger.Log(level, "Kafka: {Message}", logMessage.Message);
                })
                .Build();

            return producer;
        });

        // Register EventBus
        services.AddSingleton<IEventBus, KafkaEventBus>();

        return services;
    }

    public static IServiceCollection AddKafkaEventBusConsumer<THandler>(
        this IServiceCollection services,
        string bootstrapServers,
        string groupId,
        string topic)
        where THandler : class, IIntegrationEventHandler
    {
        services.AddScoped<THandler>();

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false, // Manual commit for reliability
            EnablePartitionEof = true,
            
            // Consumer tuning
            FetchMinBytes = 1,
            FetchWaitMaxMs = 100,
            
            // Session management
            SessionTimeoutMs = 45000,
            HeartbeatIntervalMs = 3000,
            MaxPollIntervalMs = 300000
        };

        // Register background service for consuming
        services.AddHostedService(sp => 
            new KafkaEventBusConsumer<THandler>(
                sp,
                consumerConfig,
                topic,
                sp.GetRequiredService<ILogger<KafkaEventBusConsumer<THandler>>>()));

        return services;
    }
}
