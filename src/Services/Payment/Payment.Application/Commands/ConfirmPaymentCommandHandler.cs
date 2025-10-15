namespace Payment.Application.Commands;

/// <summary>
/// Handler for confirming a payment succeeded
/// </summary>
public sealed class ConfirmPaymentCommandHandler : ICommandHandler<ConfirmPaymentCommand>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<ConfirmPaymentCommandHandler> _logger;

    public ConfirmPaymentCommandHandler(
        IPaymentRepository paymentRepository,
        ILogger<ConfirmPaymentCommandHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(
        ConfirmPaymentCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Confirming payment with external ID {ExternalPaymentId}",
            request.ExternalPaymentId);

        // Find payment by external ID
        var payment = await _paymentRepository.GetByExternalIdAsync(
            request.ExternalPaymentId,
            cancellationToken);

        if (payment is null)
        {
            _logger.LogWarning(
                "Payment with external ID {ExternalPaymentId} not found",
                request.ExternalPaymentId);

            return Result.Failure(Error.NotFound(
                "Payment", request.ExternalPaymentId));
        }

        // Mark as succeeded
        var result = payment.MarkAsSucceeded();
        if (result.IsFailure)
            return result;

        // Save changes
        await _paymentRepository.UpdateAsync(payment, cancellationToken);
        await _paymentRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Payment {PaymentId} confirmed successfully",
            payment.Id);

        return Result.Success();
    }
}
