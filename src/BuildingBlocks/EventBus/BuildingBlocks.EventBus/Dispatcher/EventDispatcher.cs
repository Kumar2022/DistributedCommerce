using BuildingBlocks.EventBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.EventBus.Dispatcher;

/// <summary>
/// Routes incoming events to registered handlers based on EventType
/// FAANG-scale pattern: Single consumer per topic + dispatcher for ordering guarantees
/// </summary>
public interface IEventDispatcher
{
    /// <summary>
    /// Register a handler for a specific event type
    /// </summary>
    void RegisterHandler<TEvent>(string eventType, IIntegrationEventHandler<TEvent> handler)
        where TEvent : IntegrationEvent;

    /// <summary>
    /// Dispatch an event to its registered handler(s)
    /// </summary>
    Task DispatchAsync(string eventType, string eventPayload, CancellationToken cancellationToken = default);
}

/// <summary>
/// Event dispatcher implementation with type-based routing
/// </summary>
public class EventDispatcher : IEventDispatcher
{
    private readonly Dictionary<string, Func<string, CancellationToken, Task>> _handlers = new();
    private readonly ILogger<EventDispatcher> _logger;
    private readonly IServiceProvider _serviceProvider;

    public EventDispatcher(
        ILogger<EventDispatcher> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public void RegisterHandler<TEvent>(string eventType, IIntegrationEventHandler<TEvent> handler)
        where TEvent : IntegrationEvent
    {
        if (_handlers.ContainsKey(eventType))
        {
            _logger.LogWarning("Handler for event type {EventType} is already registered. Replacing...", eventType);
        }

        _handlers[eventType] = async (payload, cancellationToken) =>
        {
            try
            {
                var @event = System.Text.Json.JsonSerializer.Deserialize<TEvent>(payload);
                if (@event == null)
                {
                    _logger.LogError("Failed to deserialize event of type {EventType}", eventType);
                    return;
                }

                await handler.HandleAsync(@event, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling event of type {EventType}", eventType);
                throw;
            }
        };

        _logger.LogInformation("Registered handler for event type: {EventType}", eventType);
    }

    public async Task DispatchAsync(string eventType, string eventPayload, CancellationToken cancellationToken = default)
    {
        if (!_handlers.TryGetValue(eventType, out var handler))
        {
            _logger.LogWarning("No handler registered for event type: {EventType}. Event will be ignored.", eventType);
            return;
        }

        _logger.LogDebug("Dispatching event of type: {EventType}", eventType);

        try
        {
            await handler(eventPayload, cancellationToken);
            
            _logger.LogDebug("Successfully dispatched event of type: {EventType}", eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch event of type: {EventType}", eventType);
            throw;
        }
    }
}

/// <summary>
/// Extension methods for registering event dispatcher
/// </summary>
public static class EventDispatcherExtensions
{
    public static IServiceCollection AddEventDispatcher(this IServiceCollection services)
    {
        services.AddSingleton<IEventDispatcher, EventDispatcher>();
        return services;
    }
}
