namespace Payment.Application.Commands;

/// <summary>
/// Handler for refunding a payment
/// </summary>
public sealed class RefundPaymentCommandHandler : ICommandHandler<RefundPaymentCommand>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly ILogger<RefundPaymentCommandHandler> _logger;

    public RefundPaymentCommandHandler(
        IPaymentRepository paymentRepository,
        IStripePaymentService stripePaymentService,
        ILogger<RefundPaymentCommandHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _stripePaymentService = stripePaymentService;
        _logger = logger;
    }

    public async Task<Result> Handle(
        RefundPaymentCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Refunding payment {PaymentId} with amount {RefundAmount}",
            request.PaymentId, request.RefundAmount);

        // Get payment
        var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);
        if (payment is null)
        {
            return Result.Failure(Error.NotFound(
                "Payment", request.PaymentId));
        }

        // Refund in domain
        var refundResult = payment.Refund(request.RefundAmount, request.Reason);
        if (refundResult.IsFailure)
            return refundResult;

        try
        {
            // Process refund with Stripe
            var stripeRefundId = await _stripePaymentService.RefundPaymentAsync(
                payment.ExternalPaymentId!,
                request.RefundAmount,
                request.Reason,
                cancellationToken);

            // Save changes
            await _paymentRepository.UpdateAsync(payment, cancellationToken);
            await _paymentRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Payment {PaymentId} refunded successfully with Stripe refund ID {StripeRefundId}",
                payment.Id, stripeRefundId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to refund payment {PaymentId} with Stripe",
                request.PaymentId);

            return Result.Failure(Error.Conflict(
                $"Failed to refund payment: {ex.Message}"));
        }
    }
}
