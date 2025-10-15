using BuildingBlocks.EventBus.Abstractions;
using BuildingBlocks.Saga.Abstractions;
using Microsoft.Extensions.Logging;
using Order.Domain.Events;

namespace Order.Application.Sagas.Steps;

/// <summary>
/// Step 2: Process payment for the order
/// </summary>
public class ProcessPaymentStep : ISagaStep<OrderCreationSagaState>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<ProcessPaymentStep> _logger;

    public string StepName => "ProcessPayment";

    public ProcessPaymentStep(
        IEventBus eventBus,
        ILogger<ProcessPaymentStep> logger)
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
                "Processing payment for Order {OrderId}, Amount: {Amount}",
                state.OrderId, state.TotalAmount);

            // Publish event to process payment
            var paymentId = Guid.NewGuid();
            var paymentEvent = new PaymentRequestedEvent(
                OrderId: state.OrderId,
                CustomerId: state.CustomerId,
                PaymentId: paymentId,
                Amount: state.TotalAmount,
                Currency: "USD",
                RequestedAt: DateTime.UtcNow
            );

            await _eventBus.PublishAsync(paymentEvent, cancellationToken);

            // In a real implementation, you would wait for a response event
            // For now, we'll simulate success
            state.PaymentId = paymentId;
            state.IsPaymentProcessed = true;

            _logger.LogInformation(
                "Payment processed successfully for Order {OrderId}, PaymentId: {PaymentId}",
                state.OrderId, state.PaymentId);

            return SagaStepResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process payment for Order {OrderId}", state.OrderId);
            return SagaStepResult.Failure($"Payment processing failed: {ex.Message}", ex);
        }
    }

    public async Task CompensateAsync(
        OrderCreationSagaState state, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (state.PaymentId.HasValue)
            {
                _logger.LogInformation(
                    "Refunding payment {PaymentId} for Order {OrderId}",
                    state.PaymentId, state.OrderId);

                var refundId = Guid.NewGuid();
                var refundEvent = new PaymentRefundRequestedEvent(
                    OrderId: state.OrderId,
                    PaymentId: state.PaymentId.Value,
                    RefundId: refundId,
                    Amount: state.TotalAmount,
                    Reason: "Order saga compensation",
                    RequestedAt: DateTime.UtcNow
                );

                await _eventBus.PublishAsync(refundEvent, cancellationToken);
                state.IsPaymentRefunded = true;

                _logger.LogInformation(
                    "Payment {PaymentId} refunded for Order {OrderId}",
                    state.PaymentId, state.OrderId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to refund payment {PaymentId} for Order {OrderId}",
                state.PaymentId, state.OrderId);
            throw;
        }
    }
}

// Domain Events for Payment
public record PaymentRequestedEvent(
    Guid OrderId,
    Guid CustomerId,
    Guid PaymentId,
    decimal Amount,
    string Currency,
    DateTime RequestedAt
) : IntegrationEvent(OrderId);

public record PaymentRefundRequestedEvent(
    Guid OrderId,
    Guid PaymentId,
    Guid RefundId,
    decimal Amount,
    string Reason,
    DateTime RequestedAt
) : IntegrationEvent(OrderId);
