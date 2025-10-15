using BuildingBlocks.EventBus.Kafka;
using BuildingBlocks.Infrastructure.DeadLetterQueue;
using BuildingBlocks.Infrastructure.Outbox;
using Catalog.Infrastructure.BackgroundServices;
using Catalog.Infrastructure.EventHandlers;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Infrastructure;

/// <summary>
/// Extension methods for configuring Catalog infrastructure services
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add DbContext
        var connectionString = configuration.GetConnectionString("CatalogDb");
        services.AddDbContext<CatalogDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "catalog");
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });
        });

        // Domain Repositories
        services.AddScoped<ICatalogProductRepository, CatalogProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();

        // Outbox/DLQ Repositories (Catalog doesn't consume events, no Inbox needed)
        services.AddScoped<IOutboxRepository, CatalogOutboxRepository>();
        services.AddScoped<IDeadLetterQueueRepository, CatalogDeadLetterQueueRepository>();
        services.AddScoped<IDeadLetterQueueService>(sp =>
            new DeadLetterQueueService(
                sp.GetRequiredService<IDeadLetterQueueRepository>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DeadLetterQueueService>>(),
                "catalog-service"));

        // Add Kafka Event Bus
        var kafkaBootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var kafkaClientId = configuration["Kafka:ClientId"] ?? "catalog-service";
        
        services.AddKafkaEventBus(kafkaBootstrapServers, kafkaClientId);

        // Register domain event handlers for publishing integration events to Kafka
        services.AddScoped<ProductCreatedDomainEventHandler>();
        services.AddScoped<ProductPriceChangedDomainEventHandler>();
        services.AddScoped<ProductPublishedDomainEventHandler>();
        services.AddScoped<ProductUnpublishedDomainEventHandler>();

        // Background Services - Transactional Outbox Pattern
        services.AddHostedService<OutboxMessageProcessor>();

        return services;
    }
}
