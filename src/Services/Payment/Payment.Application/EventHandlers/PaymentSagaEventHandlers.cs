using BuildingBlocks.EventBus.Abstractions;
using Microsoft.Extensions.Logging;
using Payment.Application.Commands;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;
using PaymentAggregate = Payment.Domain.Aggregates.PaymentAggregate;

namespace Payment.Application.EventHandlers;

/// <summary>
/// Handles payment request from Order Saga
/// </summary>
public class PaymentRequestedEventHandler : IIntegrationEventHandler<PaymentRequestedEvent>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<PaymentRequestedEventHandler> _logger;

    public PaymentRequestedEventHandler(
        IPaymentRepository paymentRepository,
        IEventBus eventBus,
        ILogger<PaymentRequestedEventHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task HandleAsync(PaymentRequestedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing payment request for Order {OrderId}, PaymentId: {PaymentId}, Amount: {Amount} {Currency}",
            @event.OrderId, @event.PaymentId, @event.Amount, @event.Currency);

        try
        {
            // Create value objects
            var orderId = OrderId.Create(@event.OrderId);
            var moneyResult = Money.Create(@event.Amount, @event.Currency);

            if (moneyResult.IsFailure)
            {
                var failedEvent = new PaymentFailedEvent(
                    OrderId: @event.OrderId,
                    PaymentId: @event.PaymentId,
                    Reason: moneyResult.Error.Message,
                    FailedAt: DateTime.UtcNow
                );
                await _eventBus.PublishAsync(failedEvent, cancellationToken);
                return;
            }

            // Create payment aggregate
            var paymentResult = PaymentAggregate.Payment.Create(
                orderId,
                moneyResult.Value,
                PaymentMethod.CreditCard);

            if (paymentResult.IsFailure)
            {
                var failedEvent = new PaymentFailedEvent(
                    OrderId: @event.OrderId,
                    PaymentId: @event.PaymentId,
                    Reason: paymentResult.Error.Message,
                    FailedAt: DateTime.UtcNow
                );
                await _eventBus.PublishAsync(failedEvent, cancellationToken);
                return;
            }

            var payment = paymentResult.Value;

            // Simulate payment processing (in real world, call payment gateway like Stripe)
            var processResult = payment.InitiateProcessing("SIMULATED_TRANSACTION_ID");

            if (processResult.IsSuccess)
            {
                // Mark as completed (in real world, would be done via webhook)
                var completeResult = payment.MarkAsSucceeded();
                
                if (completeResult.IsSuccess)
                {
                    await _paymentRepository.AddAsync(payment, cancellationToken);

                    // Publish success event
                    var confirmedEvent = new PaymentConfirmedEvent(
                        OrderId: @event.OrderId,
                        PaymentId: payment.Id,
                        TransactionId: "SIMULATED_TRANSACTION_ID",
                        Amount: @event.Amount,
                        Currency: @event.Currency,
                        ConfirmedAt: DateTime.UtcNow
                    );

                    await _eventBus.PublishAsync(confirmedEvent, cancellationToken);

                    _logger.LogInformation(
                        "Payment confirmed for Order {OrderId}, PaymentId: {PaymentId}",
                        @event.OrderId, payment.Id);
                }
                else
                {
                    await PublishFailureEvent(@event, completeResult.Error.Message, cancellationToken);
                }
            }
            else
            {
                await PublishFailureEvent(@event, processResult.Error.Message, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing payment for Order {OrderId}, PaymentId: {PaymentId}",
                @event.OrderId, @event.PaymentId);

            await PublishFailureEvent(@event, $"Error: {ex.Message}", cancellationToken);
            throw;
        }
    }

    private async Task PublishFailureEvent(PaymentRequestedEvent @event, string reason, CancellationToken cancellationToken)
    {
        var failedEvent = new PaymentFailedEvent(
            OrderId: @event.OrderId,
            PaymentId: @event.PaymentId,
            Reason: reason,
            FailedAt: DateTime.UtcNow
        );

        await _eventBus.PublishAsync(failedEvent, cancellationToken);

        _logger.LogWarning(
            "Payment failed for Order {OrderId}, PaymentId: {PaymentId}: {Reason}",
            @event.OrderId, @event.PaymentId, reason);
    }
}

/// <summary>
/// Handles payment refund request from Order Saga (compensation)
/// </summary>
public class PaymentRefundRequestedEventHandler : IIntegrationEventHandler<PaymentRefundRequestedEvent>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<PaymentRefundRequestedEventHandler> _logger;

    public PaymentRefundRequestedEventHandler(
        IPaymentRepository paymentRepository,
        IEventBus eventBus,
        ILogger<PaymentRefundRequestedEventHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task HandleAsync(PaymentRefundRequestedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing payment refund for Order {OrderId}, PaymentId: {PaymentId}, RefundId: {RefundId}, Reason: {Reason}",
            @event.OrderId, @event.PaymentId, @event.RefundId, @event.Reason);

        try
        {
            var payment = await _paymentRepository.GetByIdAsync(@event.PaymentId, cancellationToken);

            if (payment == null)
            {
                _logger.LogWarning(
                    "Payment {PaymentId} not found for refund, Order {OrderId}",
                    @event.PaymentId, @event.OrderId);

                // Publish refund confirmed anyway (idempotency - compensating transaction should always succeed)
                var confirmedEvent = new PaymentRefundConfirmedEvent(
                    OrderId: @event.OrderId,
                    PaymentId: @event.PaymentId,
                    RefundId: @event.RefundId,
                    Amount: @event.Amount,
                    ConfirmedAt: DateTime.UtcNow
                );

                await _eventBus.PublishAsync(confirmedEvent, cancellationToken);
                return;
            }

            // Refund payment
            var refundResult = payment.Refund(@event.Amount, @event.Reason);

            if (refundResult.IsSuccess)
            {
                await _paymentRepository.UpdateAsync(payment, cancellationToken);

                // Publish success event
                var confirmedEvent = new PaymentRefundConfirmedEvent(
                    OrderId: @event.OrderId,
                    PaymentId: @event.PaymentId,
                    RefundId: @event.RefundId,
                    Amount: @event.Amount,
                    ConfirmedAt: DateTime.UtcNow
                );

                await _eventBus.PublishAsync(confirmedEvent, cancellationToken);

                _logger.LogInformation(
                    "Payment refunded for Order {OrderId}, PaymentId: {PaymentId}, RefundId: {RefundId}",
                    @event.OrderId, @event.PaymentId, @event.RefundId);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to refund payment {PaymentId} for Order {OrderId}: {Error}",
                    @event.PaymentId, @event.OrderId, refundResult.Error.Message);

                // Still publish confirmed event (compensating transaction should always succeed)
                var confirmedEvent = new PaymentRefundConfirmedEvent(
                    OrderId: @event.OrderId,
                    PaymentId: @event.PaymentId,
                    RefundId: @event.RefundId,
                    Amount: @event.Amount,
                    ConfirmedAt: DateTime.UtcNow
                );

                await _eventBus.PublishAsync(confirmedEvent, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing payment refund for Order {OrderId}, PaymentId: {PaymentId}",
                @event.OrderId, @event.PaymentId);
            throw;
        }
    }
}

// Event definitions matching the Order Saga
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

// Response events
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

public record PaymentRefundConfirmedEvent(
    Guid OrderId,
    Guid PaymentId,
    Guid RefundId,
    decimal Amount,
    DateTime ConfirmedAt
) : IntegrationEvent(OrderId);
