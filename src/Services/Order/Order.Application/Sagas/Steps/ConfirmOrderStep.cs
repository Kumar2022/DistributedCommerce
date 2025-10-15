using BuildingBlocks.EventBus.Abstractions;
using BuildingBlocks.Saga.Abstractions;
using Microsoft.Extensions.Logging;
using Order.Domain.Events;

namespace Order.Application.Sagas.Steps;

/// <summary>
/// Step 3: Confirm the order after successful inventory reservation and payment
/// </summary>
public class ConfirmOrderStep : ISagaStep<OrderCreationSagaState>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<ConfirmOrderStep> _logger;

    public string StepName => "ConfirmOrder";

    public ConfirmOrderStep(
        IEventBus eventBus,
        ILogger<ConfirmOrderStep> logger)
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
                "Confirming Order {OrderId}",
                state.OrderId);

            // Publish event to confirm order
            var confirmedEvent = new OrderConfirmedEvent(
                OrderId: state.OrderId,
                CustomerId: state.CustomerId,
                TotalAmount: state.TotalAmount,
                ReservationId: state.ReservationId!.Value,
                PaymentId: state.PaymentId!.Value,
                ConfirmedAt: DateTime.UtcNow
            );

            await _eventBus.PublishAsync(confirmedEvent, cancellationToken);
            state.IsOrderConfirmed = true;

            _logger.LogInformation(
                "Order {OrderId} confirmed successfully",
                state.OrderId);

            return SagaStepResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm Order {OrderId}", state.OrderId);
            return SagaStepResult.Failure($"Order confirmation failed: {ex.Message}", ex);
        }
    }

    public async Task CompensateAsync(
        OrderCreationSagaState state, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Cancelling Order {OrderId}",
                state.OrderId);

            var cancelledEvent = new OrderCancelledEvent(
                OrderId: state.OrderId,
                CustomerId: state.CustomerId,
                Reason: "Order saga compensation",
                CancelledAt: DateTime.UtcNow
            );

            await _eventBus.PublishAsync(cancelledEvent, cancellationToken);
            state.IsOrderCancelled = true;

            _logger.LogInformation(
                "Order {OrderId} cancelled",
                state.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to cancel Order {OrderId}",
                state.OrderId);
            throw;
        }
    }
}

// Domain Events for Order
public record OrderConfirmedEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    Guid ReservationId,
    Guid PaymentId,
    DateTime ConfirmedAt
) : IntegrationEvent(OrderId);

public record OrderCancelledEvent(
    Guid OrderId,
    Guid CustomerId,
    string Reason,
    DateTime CancelledAt
) : IntegrationEvent(OrderId);
