namespace Payment.Application.Commands;

/// <summary>
/// Handler for processing a payment with Stripe
/// </summary>
public sealed class ProcessPaymentCommandHandler : ICommandHandler<ProcessPaymentCommand>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly ILogger<ProcessPaymentCommandHandler> _logger;

    public ProcessPaymentCommandHandler(
        IPaymentRepository paymentRepository,
        IStripePaymentService stripePaymentService,
        ILogger<ProcessPaymentCommandHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _stripePaymentService = stripePaymentService;
        _logger = logger;
    }

    public async Task<Result> Handle(
        ProcessPaymentCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing payment {PaymentId} with payment method {PaymentMethodId}",
            request.PaymentId, request.PaymentMethodId);

        // Get payment
        var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);
        if (payment is null)
        {
            return Result.Failure(Error.NotFound(
                "Payment", request.PaymentId));
        }

        try
        {
            // Create payment intent with Stripe
            var stripePaymentIntentId = await _stripePaymentService.CreatePaymentIntentAsync(
                payment.Amount.Amount,
                payment.Amount.Currency,
                request.PaymentMethodId,
                payment.Id.ToString(),
                cancellationToken);

            // Update payment with external ID
            var initiateResult = payment.InitiateProcessing(stripePaymentIntentId);
            if (initiateResult.IsFailure)
                return initiateResult;

            // Save changes
            await _paymentRepository.UpdateAsync(payment, cancellationToken);
            await _paymentRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Payment {PaymentId} initiated with Stripe payment intent {StripePaymentIntentId}",
                payment.Id, stripePaymentIntentId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process payment {PaymentId} with Stripe",
                request.PaymentId);

            // Mark payment as failed
            var failResult = payment.MarkAsFailed(
                ex.Message,
                ex.GetType().Name);

            if (failResult.IsFailure)
                return failResult;

            await _paymentRepository.UpdateAsync(payment, cancellationToken);
            await _paymentRepository.SaveChangesAsync(cancellationToken);

            // Return failure with descriptive message
            return Result.Failure(Error.Conflict(
                $"Failed to process payment: {ex.Message}"));
        }
    }
}

/// <summary>
/// Interface for Stripe payment service
/// </summary>
public interface IStripePaymentService
{
    Task<string> CreatePaymentIntentAsync(
        decimal amount,
        string currency,
        string paymentMethodId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<bool> ConfirmPaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default);

    Task<string> RefundPaymentAsync(
        string paymentIntentId,
        decimal amount,
        string reason,
        CancellationToken cancellationToken = default);
}
