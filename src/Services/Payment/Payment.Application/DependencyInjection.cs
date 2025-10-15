using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using System.Reflection;
using BuildingBlocks.EventBus.Abstractions;
using Payment.Application.EventHandlers;

namespace Payment.Application;

/// <summary>
/// Dependency injection configuration for Payment Application layer
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPaymentApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // Register Event Handlers
        services.AddScoped<IIntegrationEventHandler<PaymentRequestedEvent>, PaymentRequestedEventHandler>();
        services.AddScoped<IIntegrationEventHandler<PaymentRefundRequestedEvent>, PaymentRefundRequestedEventHandler>();

        return services;
    }
}
