using BuildingBlocks.Domain.Aggregates;
using Payment.Domain.Enums;
using Payment.Domain.Events;
using Payment.Domain.ValueObjects;

namespace Payment.Domain.Aggregates.PaymentAggregate;

/// <summary>
/// Payment aggregate root
/// Handles payment processing with external payment providers (Stripe)
/// Implements transactional outbox pattern for reliable event publishing
/// </summary>
public sealed class Payment : AggregateRoot<Guid>
{
    public OrderId OrderId { get; private set; } = null!;
    public Money Amount { get; private set; } = null!;
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string? ExternalPaymentId { get; private set; }
    public string? FailureReason { get; private set; }
    public string? ErrorCode { get; private set; }
    public decimal RefundedAmount { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public DateTime? FailedAt { get; private set; }
    public DateTime? RefundedAt { get; private set; }

    // EF Core constructor
    private Payment() { }

    /// <summary>
    /// Create a new payment
    /// </summary>
    public static Result<Payment> Create(
        OrderId orderId,
        Money amount,
        PaymentMethod method)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Amount = amount,
            Method = method,
            Status = PaymentStatus.Pending,
            RefundedAmount = 0,
            CreatedAt = DateTime.UtcNow
        };

        payment.AddDomainEvent(new PaymentCreatedEvent(
            payment.Id,
            orderId.Value,
            amount.Amount,
            amount.Currency,
            method.ToString(),
            payment.CreatedAt));

        return Result.Success(payment);
    }

    /// <summary>
    /// Initiate payment processing with external provider
    /// </summary>
    public Result InitiateProcessing(string externalPaymentId)
    {
        if (Status != PaymentStatus.Pending)
            return Result.Failure(Error.Conflict(
                $"Payment is already in {Status} status"));

        if (string.IsNullOrWhiteSpace(externalPaymentId))
            return Result.Failure(Error.Validation(
                "ExternalPaymentId",
                "External payment ID is required"));

        Status = PaymentStatus.Processing;
        ExternalPaymentId = externalPaymentId;

        AddDomainEvent(new PaymentProcessingInitiatedEvent(
            Id,
            externalPaymentId,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Mark payment as succeeded
    /// </summary>
    public Result MarkAsSucceeded()
    {
        if (Status != PaymentStatus.Processing)
            return Result.Failure(Error.Conflict(
                $"Cannot mark payment as succeeded from {Status} status"));

        Status = PaymentStatus.Succeeded;
        ProcessedAt = DateTime.UtcNow;

        AddDomainEvent(new PaymentSucceededEvent(
            Id,
            OrderId.Value,
            ExternalPaymentId!,
            Amount.Amount,
            Amount.Currency,
            ProcessedAt.Value));

        return Result.Success();
    }

    /// <summary>
    /// Mark payment as failed
    /// </summary>
    public Result MarkAsFailed(string reason, string errorCode)
    {
        if (Status == PaymentStatus.Succeeded)
            return Result.Failure(Error.Conflict(
                "Cannot fail a succeeded payment"));

        Status = PaymentStatus.Failed;
        FailureReason = reason;
        ErrorCode = errorCode;
        FailedAt = DateTime.UtcNow;

        AddDomainEvent(new PaymentFailedEvent(
            Id,
            OrderId.Value,
            reason,
            errorCode,
            FailedAt.Value));

        return Result.Success();
    }

    /// <summary>
    /// Refund the payment (full or partial)
    /// </summary>
    public Result Refund(decimal refundAmount, string reason)
    {
        // Allow refunds on succeeded or partially refunded payments
        if (Status != PaymentStatus.Succeeded && Status != PaymentStatus.PartiallyRefunded)
            return Result.Failure(Error.Conflict(
                $"Cannot refund payment in {Status} status. Only succeeded or partially refunded payments can be refunded."));

        if (refundAmount <= 0)
            return Result.Failure(Error.Validation(
                "RefundAmount",
                "Refund amount must be positive"));

        var totalRefunded = RefundedAmount + refundAmount;
        if (totalRefunded > Amount.Amount)
            return Result.Failure(Error.Validation(
                "RefundAmount",
                $"Total refund ({totalRefunded}) exceeds payment amount ({Amount.Amount})"));

        RefundedAmount += refundAmount;
        RefundedAt = DateTime.UtcNow;

        Status = RefundedAmount >= Amount.Amount
            ? PaymentStatus.Refunded
            : PaymentStatus.PartiallyRefunded;

        AddDomainEvent(new PaymentRefundedEvent(
            Id,
            OrderId.Value,
            refundAmount,
            Amount.Currency,
            reason,
            RefundedAt.Value));

        return Result.Success();
    }
}
