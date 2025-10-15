using BuildingBlocks.EventBus.Kafka;
using BuildingBlocks.Infrastructure.DeadLetterQueue;
using BuildingBlocks.Infrastructure.Outbox;
using Identity.Application.DTOs;
using Identity.Application.Services;
using Identity.Infrastructure.BackgroundServices;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence.Repositories;
using Identity.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure;

/// <summary>
/// Dependency injection configuration for Infrastructure layer
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("IdentityDb") ?? 
                "Host=localhost;Database=DistributedCommerce_Identity;Username=postgres;Password=postgres",
                npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
                    npgsqlOptions.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName);
                }));

        // Domain Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IdentityDbContext>());

        // Outbox/DLQ Repositories (Identity publishes user events, doesn't consume)
        services.AddScoped<IOutboxRepository, IdentityOutboxRepository>();
        services.AddScoped<IDeadLetterQueueRepository, IdentityDeadLetterQueueRepository>();
        services.AddScoped<IDeadLetterQueueService>(sp =>
            new DeadLetterQueueService(
                sp.GetRequiredService<IDeadLetterQueueRepository>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DeadLetterQueueService>>(),
                "identity-service"));

        // Services
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        // Kafka Event Bus
        var kafkaBootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var kafkaClientId = configuration["Kafka:ClientId"] ?? "identity-service";
        
        services.AddKafkaEventBus(kafkaBootstrapServers, kafkaClientId);

        // Background Services
        services.AddHostedService<OutboxMessageProcessor>();

        return services;
    }
}
