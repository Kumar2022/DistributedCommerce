using BuildingBlocks.EventBus.Kafka;
using BuildingBlocks.Infrastructure.DeadLetterQueue;
using BuildingBlocks.Infrastructure.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notification.Domain.Repositories;
using Notification.Infrastructure.Persistence;
using Notification.Infrastructure.Persistence.Repositories;
using Notification.Infrastructure.Repositories;
using Notification.Infrastructure.Services;

namespace Notification.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<NotificationDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("NotificationDb"),
                npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
                });
        });

        // Domain Repositories
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();

        // Inbox/DLQ Repositories (Notification is consumer-only, no Outbox needed)
        services.AddScoped<IInboxRepository, NotificationInboxRepository>();
        services.AddScoped<IDeadLetterQueueRepository, NotificationDeadLetterQueueRepository>();
        services.AddScoped<IDeadLetterQueueService>(sp =>
            new DeadLetterQueueService(
                sp.GetRequiredService<IDeadLetterQueueRepository>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DeadLetterQueueService>>(),
                "notification-service"));

        // Notification Senders
        services.AddSingleton<INotificationSender, EmailNotificationSender>();
        services.AddSingleton<INotificationSender, SmsNotificationSender>();
        services.AddSingleton<INotificationSender, PushNotificationSender>();
        services.AddSingleton<INotificationSenderFactory, NotificationSenderFactory>();

        // Event Bus (Kafka)
        var kafkaBootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var kafkaClientId = configuration["Kafka:ClientId"] ?? "notification-service";
        
        services.AddKafkaEventBus(kafkaBootstrapServers, kafkaClientId);

        return services;
    }
}
