using BuildingBlocks.EventBus.Abstractions;
using Microsoft.Extensions.Logging;
using Order.Application.DTOs;
using Order.Application.Sagas.Steps;

namespace Order.Application.EventHandlers;

/// <summary>
/// Handles order confirmation event (final step of Order Saga)
/// </summary>
public class OrderConfirmedEventHandler : IIntegrationEventHandler<OrderConfirmedEvent>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<OrderConfirmedEventHandler> _logger;

    public OrderConfirmedEventHandler(
        IOrderRepository orderRepository,
        ILogger<OrderConfirmedEventHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task HandleAsync(OrderConfirmedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing order confirmation for Order {OrderId}",
            @event.OrderId);

        try
        {
            var order = await _orderRepository.GetByIdAsync(@event.OrderId, cancellationToken);

            if (order == null)
            {
                _logger.LogWarning(
                    "Order {OrderId} not found for confirmation",
                    @event.OrderId);
                return;
            }

            // Complete payment first
            var paymentResult = order.CompletePayment(@event.PaymentId);
            if (paymentResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to complete payment for Order {OrderId}: {Error}",
                    @event.OrderId, paymentResult.Error.Message);
                return;
            }

            // Then confirm order
            var confirmResult = order.Confirm();

            if (confirmResult.IsSuccess)
            {
                await _orderRepository.UpdateAsync(order, cancellationToken);

                _logger.LogInformation(
                    "Order {OrderId} confirmed successfully. Status: {Status}",
                    @event.OrderId, order.Status);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to confirm Order {OrderId}: {Error}",
                    @event.OrderId, confirmResult.Error.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing order confirmation for Order {OrderId}",
                @event.OrderId);
            throw;
        }
    }
}

/// <summary>
/// Handles order cancellation event (compensation from Order Saga)
/// </summary>
public class OrderCancelledEventHandler : IIntegrationEventHandler<OrderCancelledEvent>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<OrderCancelledEventHandler> _logger;

    public OrderCancelledEventHandler(
        IOrderRepository orderRepository,
        ILogger<OrderCancelledEventHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task HandleAsync(OrderCancelledEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing order cancellation for Order {OrderId}, Reason: {Reason}",
            @event.OrderId, @event.Reason);

        try
        {
            var order = await _orderRepository.GetByIdAsync(@event.OrderId, cancellationToken);

            if (order == null)
            {
                _logger.LogWarning(
                    "Order {OrderId} not found for cancellation",
                    @event.OrderId);
                return;
            }

            // Cancel order
            var result = order.Cancel(@event.Reason);

            if (result.IsSuccess)
            {
                await _orderRepository.UpdateAsync(order, cancellationToken);

                _logger.LogInformation(
                    "Order {OrderId} cancelled successfully. Status: {Status}, Reason: {Reason}",
                    @event.OrderId, order.Status, @event.Reason);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to cancel Order {OrderId}: {Error}",
                    @event.OrderId, result.Error.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing order cancellation for Order {OrderId}",
                @event.OrderId);
            throw;
        }
    }
}

/// <summary>
/// Handles inventory reservation confirmed event
/// Updates order state when inventory is reserved
/// </summary>
public class InventoryReservationConfirmedEventHandler : IIntegrationEventHandler<InventoryReservationConfirmedEvent>
{
    private readonly ILogger<InventoryReservationConfirmedEventHandler> _logger;

    public InventoryReservationConfirmedEventHandler(
        ILogger<InventoryReservationConfirmedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(InventoryReservationConfirmedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Inventory reservation confirmed for Order {OrderId}, ReservationId: {ReservationId}",
            @event.OrderId, @event.ReservationId);

        // Additional logic if needed (e.g., update saga state from event bus)
        return Task.CompletedTask;
    }
}

/// <summary>
/// Handles inventory reservation failed event
/// Triggers saga compensation
/// </summary>
public class InventoryReservationFailedEventHandler : IIntegrationEventHandler<InventoryReservationFailedEvent>
{
    private readonly ILogger<InventoryReservationFailedEventHandler> _logger;

    public InventoryReservationFailedEventHandler(
        ILogger<InventoryReservationFailedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(InventoryReservationFailedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Inventory reservation failed for Order {OrderId}, ReservationId: {ReservationId}, Reason: {Reason}",
            @event.OrderId, @event.ReservationId, @event.Reason);

        // Additional logic if needed (e.g., trigger saga compensation)
        return Task.CompletedTask;
    }
}

/// <summary>
/// Handles payment confirmed event
/// </summary>
public class PaymentConfirmedEventHandler : IIntegrationEventHandler<PaymentConfirmedEvent>
{
    private readonly ILogger<PaymentConfirmedEventHandler> _logger;

    public PaymentConfirmedEventHandler(
        ILogger<PaymentConfirmedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(PaymentConfirmedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Payment confirmed for Order {OrderId}, PaymentId: {PaymentId}, TransactionId: {TransactionId}",
            @event.OrderId, @event.PaymentId, @event.TransactionId);

        // Additional logic if needed
        return Task.CompletedTask;
    }
}

/// <summary>
/// Handles payment failed event
/// Triggers saga compensation
/// </summary>
public class PaymentFailedEventHandler : IIntegrationEventHandler<PaymentFailedEvent>
{
    private readonly ILogger<PaymentFailedEventHandler> _logger;

    public PaymentFailedEventHandler(
        ILogger<PaymentFailedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(PaymentFailedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Payment failed for Order {OrderId}, PaymentId: {PaymentId}, Reason: {Reason}",
            @event.OrderId, @event.PaymentId, @event.Reason);

        // Additional logic if needed (e.g., trigger saga compensation)
        return Task.CompletedTask;
    }
}

// Response events from other services (Inventory, Payment)
// These are duplicated here for loose coupling between services

public record InventoryReservationConfirmedEvent(
    Guid OrderId,
    Guid ReservationId,
    List<InventoryReservationItem> Items,
    DateTime ConfirmedAt
) : IntegrationEvent(OrderId);

public record InventoryReservationFailedEvent(
    Guid OrderId,
    Guid ReservationId,
    string Reason,
    DateTime FailedAt
) : IntegrationEvent(OrderId);

public record PaymentConfirmedEvent(
    Guid OrderId,
    Guid PaymentId,
    string TransactionId,
    decimal Amount,
    string Currency,
    DateTime ConfirmedAt
) : IntegrationEvent(OrderId);

public record PaymentFailedEvent(
    Guid OrderId,
    Guid PaymentId,
    string Reason,
    DateTime FailedAt
) : IntegrationEvent(OrderId);

public record InventoryReservationItem(
    Guid ProductId,
    int Quantity
);
