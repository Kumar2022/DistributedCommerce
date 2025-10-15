using Analytics.Infrastructure.Persistence;
using Analytics.Infrastructure.Persistence.Repositories;
using Analytics.Infrastructure.Repositories;
using BuildingBlocks.EventBus.Kafka;
using BuildingBlocks.Infrastructure.DeadLetterQueue;
using BuildingBlocks.Infrastructure.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Analytics.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("AnalyticsDb") 
            ?? "Host=localhost;Port=5432;Database=analytics_db;Username=postgres;Password=postgres";

        services.AddDbContext<AnalyticsDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(AnalyticsDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
            });
        });

        // Domain Repositories
        services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();

        // Inbox/DLQ Repositories (Analytics is consumer-only, no Outbox needed)
        services.AddScoped<IInboxRepository, AnalyticsInboxRepository>();
        services.AddScoped<IDeadLetterQueueRepository, AnalyticsDeadLetterQueueRepository>();
        services.AddScoped<IDeadLetterQueueService>(sp =>
            new DeadLetterQueueService(
                sp.GetRequiredService<IDeadLetterQueueRepository>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DeadLetterQueueService>>(),
                "analytics-service"));

        // Kafka Event Bus
        var kafkaBootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var kafkaClientId = configuration["Kafka:ClientId"] ?? "analytics-service";
        
        services.AddKafkaEventBus(kafkaBootstrapServers, kafkaClientId);

        return services;
    }
}
