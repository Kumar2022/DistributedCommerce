using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using System.Reflection;
using BuildingBlocks.EventBus.Abstractions;
using BuildingBlocks.Application.Behaviors;
using Inventory.Application.EventHandlers;

namespace Inventory.Application;

/// <summary>
/// Dependency injection configuration for Inventory Application layer
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInventoryApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR with Behaviors
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
        });

        // FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // Register Event Handlers
        services.AddScoped<IIntegrationEventHandler<InventoryReservationRequestedEvent>, InventoryReservationRequestedEventHandler>();
        services.AddScoped<IIntegrationEventHandler<InventoryReservationReleasedEvent>, InventoryReservationReleasedEventHandler>();

        return services;
    }
}
