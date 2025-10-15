using BuildingBlocks.EventBus.Kafka;
using BuildingBlocks.Infrastructure.DeadLetterQueue;
using BuildingBlocks.Infrastructure.Inbox;
using BuildingBlocks.Infrastructure.Outbox;
using BuildingBlocks.Saga.Abstractions;
using BuildingBlocks.Saga.Storage;
using Marten;
using Marten.Events.Projections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Order.Application.DTOs;
using Order.Application.Sagas;
using Order.Infrastructure.BackgroundServices;
using Order.Infrastructure.EventStore;
using Order.Infrastructure.Persistence;
using Order.Infrastructure.Persistence.Repositories;
using Weasel.Core;

namespace Order.Infrastructure;

/// <summary>
/// Dependency injection configuration for Infrastructure layer
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddOrderInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Marten Event Store (for Order aggregate - event sourcing)
        services.AddMarten(options =>
        {
            var connectionString = configuration.GetConnectionString("OrderDb") ??
                "Host=localhost;Database=DistributedCommerce_Order;Username=postgres;Password=postgres";

            options.Connection(connectionString);

            // Auto-create database schema
            options.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;

            // Event Projections (for read models) - to be added
            // options.Projections.Add<OrderProjection>(ProjectionLifecycle.Inline);

            // Schema configuration
            options.DatabaseSchemaName = "orders";
        })
        .UseLightweightSessions(); // Scoped sessions for web apps

        // EF Core DbContext (for Outbox/Inbox/DLQ - separate from Marten)
        services.AddDbContext<OrderInfrastructureDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("OrderInfrastructureDb") ??
                "Host=localhost;Database=order_infrastructure_db;Username=postgres;Password=postgres"));

        // Saga State DbContext (for saga persistence with optimistic concurrency)
        services.AddDbContext<SagaStateDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("OrderInfrastructureDb") ??
                "Host=localhost;Database=order_infrastructure_db;Username=postgres;Password=postgres",
                b => b.MigrationsAssembly("Order.Infrastructure")));

        // Saga State Repository (PostgreSQL-backed for durability)
        services.AddScoped<ISagaStateRepository<OrderCreationSagaState>>(sp =>
            new PostgresSagaStateRepository<OrderCreationSagaState>(
                sp.GetRequiredService<SagaStateDbContext>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<PostgresSagaStateRepository<OrderCreationSagaState>>>()));

        // Domain Repositories
        services.AddScoped<IOrderRepository, OrderRepository>();

        // Outbox/Inbox/DLQ Repositories (for data consistency patterns)
        services.AddScoped<IOutboxRepository, OrderOutboxRepository>();
        services.AddScoped<IInboxRepository, OrderInboxRepository>();
        services.AddScoped<IDeadLetterQueueRepository, OrderDeadLetterQueueRepository>();
        services.AddScoped<IDeadLetterQueueService>(sp =>
            new DeadLetterQueueService(
                sp.GetRequiredService<IDeadLetterQueueRepository>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DeadLetterQueueService>>(),
                "order-service"));

        // Kafka Event Bus
        services.AddKafkaEventBus(
            configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            "order-service");

        // Background Services - Transactional Outbox Pattern and Saga Recovery
        services.AddHostedService<OutboxMessageProcessor>();
        services.AddHostedService<SagaRecoveryService>();

        return services;
    }
}
