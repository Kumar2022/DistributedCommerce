using BuildingBlocks.EventBus.Abstractions;
using BuildingBlocks.Saga.Abstractions;
using Microsoft.Extensions.Logging;
using Order.Domain.Events;

namespace Order.Application.Sagas.Steps;

/// <summary>
/// Step 1: Reserve inventory for order items
/// </summary>
public class ReserveInventoryStep : ISagaStep<OrderCreationSagaState>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<ReserveInventoryStep> _logger;

    public string StepName => "ReserveInventory";

    public ReserveInventoryStep(
        IEventBus eventBus,
        ILogger<ReserveInventoryStep> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<SagaStepResult> ExecuteAsync(
        OrderCreationSagaState state, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Reserving inventory for Order {OrderId} with {ItemCount} items",
                state.OrderId, state.OrderItems.Count);

            // Publish event to reserve inventory
            var reservationId = Guid.NewGuid();
            var reservationEvent = new InventoryReservationRequestedEvent(
                OrderId: state.OrderId,
                CustomerId: state.CustomerId,
                ReservationId: reservationId,
                Items: state.OrderItems.Select(i => new InventoryReservationItem(
                    ProductId: i.ProductId,
                    Quantity: i.Quantity
                )).ToList(),
                RequestedAt: DateTime.UtcNow
            );

            await _eventBus.PublishAsync(reservationEvent, cancellationToken);

            // In a real implementation, you would wait for a response event
            // For now, we'll simulate success
            state.ReservationId = reservationId;
            state.IsInventoryReserved = true;

            _logger.LogInformation(
                "Inventory reserved successfully for Order {OrderId}, ReservationId: {ReservationId}",
                state.OrderId, state.ReservationId);

            return SagaStepResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reserve inventory for Order {OrderId}", state.OrderId);
            return SagaStepResult.Failure($"Inventory reservation failed: {ex.Message}", ex);
        }
    }

    public async Task CompensateAsync(
        OrderCreationSagaState state, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (state.ReservationId.HasValue)
            {
                _logger.LogInformation(
                    "Releasing inventory reservation {ReservationId} for Order {OrderId}",
                    state.ReservationId, state.OrderId);

                var releaseEvent = new InventoryReservationReleasedEvent(
                    OrderId: state.OrderId,
                    ReservationId: state.ReservationId.Value,
                    Reason: "Order saga compensation",
                    ReleasedAt: DateTime.UtcNow
                );

                await _eventBus.PublishAsync(releaseEvent, cancellationToken);
                state.IsInventoryReleased = true;

                _logger.LogInformation(
                    "Inventory reservation {ReservationId} released for Order {OrderId}",
                    state.ReservationId, state.OrderId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to release inventory reservation {ReservationId} for Order {OrderId}",
                state.ReservationId, state.OrderId);
            throw;
        }
    }
}

// Domain Events for Inventory
public record InventoryReservationRequestedEvent(
    Guid OrderId,
    Guid CustomerId,
    Guid ReservationId,
    List<InventoryReservationItem> Items,
    DateTime RequestedAt
) : IntegrationEvent(OrderId);

public record InventoryReservationReleasedEvent(
    Guid OrderId,
    Guid ReservationId,
    string Reason,
    DateTime ReleasedAt
) : IntegrationEvent(OrderId);

public record InventoryReservationItem(
    Guid ProductId,
    int Quantity
);
