using BuildingBlocks.EventBus.Kafka;
using BuildingBlocks.Infrastructure.DeadLetterQueue;
using BuildingBlocks.Infrastructure.Inbox;
using BuildingBlocks.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shipping.Infrastructure.BackgroundServices;
using Shipping.Infrastructure.Persistence;
using Shipping.Infrastructure.Persistence.Repositories;
using Shipping.Infrastructure.Repositories;

namespace Shipping.Infrastructure;

/// <summary>
/// Extension methods for configuring Shipping infrastructure services
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddShippingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add DbContext
        var connectionString = configuration.GetConnectionString("ShippingDb")
            ?? throw new InvalidOperationException("ShippingDb connection string is not configured");

        services.AddDbContext<ShippingDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "shipping");
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(30);
            });
        });

        // Domain Repository
        services.AddScoped<IShipmentRepository, ShipmentRepository>();

        // Outbox/Inbox/DLQ Repositories (for data consistency patterns)
        services.AddScoped<IOutboxRepository, ShippingOutboxRepository>();
        services.AddScoped<IInboxRepository, ShippingInboxRepository>();
        services.AddScoped<IDeadLetterQueueRepository, ShippingDeadLetterQueueRepository>();
        services.AddScoped<IDeadLetterQueueService>(sp =>
            new DeadLetterQueueService(
                sp.GetRequiredService<IDeadLetterQueueRepository>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DeadLetterQueueService>>(),
                "shipping-service"));

        // Add Kafka Event Bus
        var kafkaBootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var kafkaClientId = configuration["Kafka:ClientId"] ?? "shipping-service";

        services.AddKafkaEventBus(kafkaBootstrapServers, kafkaClientId);

        // Background Services
        services.AddHostedService<OutboxMessageProcessor>();

        return services;
    }
}
