using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildingBlocks.Idempotency.Extensions;

/// <summary>
/// Extension methods for adding idempotency services to DI container
/// </summary>
public static class IdempotencyServiceCollectionExtensions
{
    /// <summary>
    /// Add idempotency support with Redis backing store
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="redisConnection">Redis connection string</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddIdempotency(
        this IServiceCollection services,
        string redisConnection)
    {
        if (string.IsNullOrWhiteSpace(redisConnection))
            throw new ArgumentException("Redis connection string cannot be null or empty", nameof(redisConnection));

        // Add Redis distributed cache
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "Idempotency:";
        });

        // Register idempotency store
        services.TryAddSingleton<IIdempotencyStore, RedisIdempotencyStore>();

        return services;
    }

    /// <summary>
    /// Add idempotency support with custom IDistributedCache implementation
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddIdempotency(this IServiceCollection services)
    {
        // Register idempotency store (assumes IDistributedCache is already registered)
        services.TryAddSingleton<IIdempotencyStore, RedisIdempotencyStore>();

        return services;
    }

    /// <summary>
    /// Register an event handler with idempotency decorator
    /// </summary>
    /// <typeparam name="TEvent">Event type</typeparam>
    /// <typeparam name="THandler">Handler type</typeparam>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddIdempotentEventHandler<TEvent, THandler>(
        this IServiceCollection services)
        where TEvent : IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        // Register the actual handler
        services.AddScoped<THandler>();

        // Register the decorated handler
        services.AddScoped<IIntegrationEventHandler<TEvent>>(sp =>
        {
            var handler = sp.GetRequiredService<THandler>();
            var idempotencyStore = sp.GetRequiredService<IIdempotencyStore>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<IdempotentEventHandlerDecorator<TEvent>>>();

            return new IdempotentEventHandlerDecorator<TEvent>(handler, idempotencyStore, logger);
        });

        return services;
    }
}
