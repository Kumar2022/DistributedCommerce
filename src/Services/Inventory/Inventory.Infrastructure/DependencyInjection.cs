using BuildingBlocks.EventBus.Kafka;
using BuildingBlocks.Infrastructure.DeadLetterQueue;
using BuildingBlocks.Infrastructure.Inbox;
using BuildingBlocks.Infrastructure.Outbox;
using Inventory.Infrastructure.BackgroundServices;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Persistence.Repositories;
using Inventory.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure;

/// <summary>
/// Infrastructure dependency injection extensions for Inventory service
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInventoryInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<InventoryDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("InventoryDb") ??
                "Host=localhost;Database=inventory_db;Username=postgres;Password=postgres"));

        // Domain Repositories
        services.AddScoped<IProductRepository, ProductRepository>();

        // Outbox/Inbox/DLQ Repositories (for data consistency patterns)
        services.AddScoped<IOutboxRepository, InventoryOutboxRepository>();
        services.AddScoped<IInboxRepository, InventoryInboxRepository>();
        services.AddScoped<IDeadLetterQueueRepository, InventoryDeadLetterQueueRepository>();
        services.AddScoped<IDeadLetterQueueService>(sp =>
            new DeadLetterQueueService(
                sp.GetRequiredService<IDeadLetterQueueRepository>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DeadLetterQueueService>>(),
                "inventory-service"));

        // Kafka Event Bus
        services.AddKafkaEventBus(
            configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            "inventory-service");

        // Background Services - Transactional Outbox Pattern
        services.AddHostedService<OutboxMessageProcessor>();

        return services;
    }
}
