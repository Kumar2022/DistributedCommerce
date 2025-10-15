using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using System.Reflection;
using BuildingBlocks.Saga.Extensions;
using BuildingBlocks.EventBus.Abstractions;
using Order.Application.Sagas;
using Order.Application.Sagas.Steps;
using Order.Application.EventHandlers;

namespace Order.Application;

/// <summary>
/// Dependency injection configuration for Application layer
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddOrderApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // Saga Support
        services.AddSagaOrchestration();
        
        // Register Saga Steps
        services.AddScoped<ReserveInventoryStep>();
        services.AddScoped<ProcessPaymentStep>();
        services.AddScoped<ConfirmOrderStep>();
        
        // Register Saga Orchestrator
        services.AddScoped<OrderCreationSaga>();

        // Register Event Handlers
        // - Order lifecycle events from saga steps
        services.AddScoped<IIntegrationEventHandler<Sagas.Steps.OrderConfirmedEvent>, OrderConfirmedEventHandler>();
        services.AddScoped<IIntegrationEventHandler<Sagas.Steps.OrderCancelledEvent>, OrderCancelledEventHandler>();
        // - Response events from other services
        services.AddScoped<IIntegrationEventHandler<EventHandlers.InventoryReservationConfirmedEvent>, InventoryReservationConfirmedEventHandler>();
        services.AddScoped<IIntegrationEventHandler<EventHandlers.InventoryReservationFailedEvent>, InventoryReservationFailedEventHandler>();
        services.AddScoped<IIntegrationEventHandler<EventHandlers.PaymentConfirmedEvent>, PaymentConfirmedEventHandler>();
        services.AddScoped<IIntegrationEventHandler<EventHandlers.PaymentFailedEvent>, PaymentFailedEventHandler>();

        return services;
    }
}
