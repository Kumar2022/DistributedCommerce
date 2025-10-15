using BuildingBlocks.Infrastructure.DeadLetterQueue;
using BuildingBlocks.Infrastructure.DistributedLock;
using BuildingBlocks.Infrastructure.Inbox;
using BuildingBlocks.Infrastructure.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering infrastructure services
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Adds Inbox pattern services for idempotent event processing
    /// </summary>
    public static IServiceCollection AddInboxPattern(
        this IServiceCollection services,
        TimeSpan? processingInterval = null)
    {
        // The repository implementation should be provided by each service
        // services.AddScoped<IInboxRepository, PostgresInboxRepository>();
        
        services.AddHostedService(sp => new InboxProcessor(
            sp,
            sp.GetRequiredService<ILogger<InboxProcessor>>(),
            processingInterval));
        
        return services;
    }

    /// <summary>
    /// Adds Outbox pattern services for transactional event publishing
    /// </summary>
    public static IServiceCollection AddOutboxPattern(
        this IServiceCollection services,
        TimeSpan? processingInterval = null)
    {
        // The repository and processor implementations should be provided by each service
        // services.AddScoped<IOutboxRepository, PostgresOutboxRepository>();
        // services.AddHostedService<OutboxMessageProcessor>();
        
        return services;
    }

    /// <summary>
    /// Adds Dead Letter Queue services for failed message handling
    /// </summary>
    public static IServiceCollection AddDeadLetterQueue(
        this IServiceCollection services,
        string serviceName)
    {
        // The repository implementation should be provided by each service
        // services.AddScoped<IDeadLetterQueueRepository, PostgresDlqRepository>();
        
        services.AddScoped<IDeadLetterQueueService>(sp =>
            new DeadLetterQueueService(
                sp.GetRequiredService<IDeadLetterQueueRepository>(),
                sp.GetRequiredService<ILogger<DeadLetterQueueService>>(),
                serviceName));
        
        return services;
    }

    /// <summary>
    /// Adds distributed locking with PostgreSQL
    /// </summary>
    public static IServiceCollection AddPostgresDistributedLock(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddSingleton<IDistributedLock>(sp =>
            new PostgresDistributedLock(
                connectionString,
                sp.GetRequiredService<ILogger<PostgresDistributedLock>>()));
        
        return services;
    }

    /// <summary>
    /// Adds distributed locking with Redis
    /// </summary>
    public static IServiceCollection AddRedisDistributedLock(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddSingleton<IDistributedLock>(sp =>
        {
            var redis = StackExchange.Redis.ConnectionMultiplexer.Connect(connectionString);
            return new RedisDistributedLock(
                redis,
                sp.GetRequiredService<ILogger<RedisDistributedLock>>());
        });
        
        return services;
    }
}
