using BuildingBlocks.EventBus.Kafka;
using BuildingBlocks.Infrastructure.Inbox;
using BuildingBlocks.Infrastructure.Outbox;
using BuildingBlocks.Infrastructure.DeadLetterQueue;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Application.Commands;
using Payment.Infrastructure.BackgroundServices;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Persistence.Repositories;
using Payment.Infrastructure.Stripe;

namespace Payment.Infrastructure;

/// <summary>
/// Infrastructure dependency injection extensions
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPaymentInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<PaymentDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("PaymentDb") ??
                "Host=localhost;Database=payment_db;Username=postgres;Password=postgres"));

        // Domain Repositories
        services.AddScoped<IPaymentRepository, PaymentRepository>();

        // Outbox/Inbox/DLQ Repositories (for data consistency patterns)
        services.AddScoped<IOutboxRepository, PaymentOutboxRepository>();
        services.AddScoped<IInboxRepository, PaymentInboxRepository>();
        services.AddScoped<IDeadLetterQueueRepository, PaymentDeadLetterQueueRepository>();
        services.AddScoped<IDeadLetterQueueService>(sp =>
            new DeadLetterQueueService(
                sp.GetRequiredService<IDeadLetterQueueRepository>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DeadLetterQueueService>>(),
                "payment-service"));

        // Stripe
        global::Stripe.StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"] ??
            throw new InvalidOperationException("Stripe:SecretKey is not configured");

        services.AddScoped<IStripePaymentService, StripePaymentService>();

        // Kafka Event Bus
        services.AddKafkaEventBus(
            configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            "payment-service");

        // Background Services
        services.AddHostedService<OutboxMessageProcessor>();

        return services;
    }
}
