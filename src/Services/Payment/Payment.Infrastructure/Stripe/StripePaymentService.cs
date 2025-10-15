using Polly;
using Polly.Retry;
using Stripe;

namespace Payment.Infrastructure.Stripe;

/// <summary>
/// Stripe payment service implementation
/// Handles payment processing with Stripe API
/// </summary>
public sealed class StripePaymentService : IStripePaymentService
{
    private readonly ILogger<StripePaymentService> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public StripePaymentService(ILogger<StripePaymentService> logger)
    {
        _logger = logger;

        // Retry policy for transient errors
        _retryPolicy = Policy
            .Handle<StripeException>(ex => ex.StripeError?.Type == "rate_limit_error" ||
                                           ex.StripeError?.Type == "api_connection_error")
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Stripe API call failed. Retry attempt {RetryCount} after {Delay}ms. Error: {Error}",
                        retryCount, timeSpan.TotalMilliseconds, exception.Message);
                });
    }

    public async Task<string> CreatePaymentIntentAsync(
        decimal amount,
        string currency,
        string paymentMethodId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Creating Stripe payment intent for amount {Amount} {Currency}",
            amount, currency);

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var service = new PaymentIntentService();

            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100), // Convert to cents
                Currency = currency.ToLower(),
                PaymentMethod = paymentMethodId,
                ConfirmationMethod = "manual",
                Confirm = false,
                Description = $"Payment for order",
                Metadata = new Dictionary<string, string>
                {
                    { "idempotency_key", idempotencyKey }
                }
            };

            var requestOptions = new RequestOptions
            {
                IdempotencyKey = idempotencyKey
            };

            var paymentIntent = await service.CreateAsync(
                options,
                requestOptions,
                cancellationToken);

            _logger.LogInformation(
                "Created Stripe payment intent {PaymentIntentId} with status {Status}",
                paymentIntent.Id, paymentIntent.Status);

            return paymentIntent.Id;
        });
    }

    public async Task<bool> ConfirmPaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Confirming Stripe payment intent {PaymentIntentId}",
            paymentIntentId);

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var service = new PaymentIntentService();

            var options = new PaymentIntentConfirmOptions
            {
                ReturnUrl = "https://example.com/payment/complete"
            };

            var paymentIntent = await service.ConfirmAsync(
                paymentIntentId,
                options,
                cancellationToken: cancellationToken);

            var success = paymentIntent.Status == "succeeded";

            _logger.LogInformation(
                "Confirmed Stripe payment intent {PaymentIntentId} with status {Status}",
                paymentIntentId, paymentIntent.Status);

            return success;
        });
    }

    public async Task<string> RefundPaymentAsync(
        string paymentIntentId,
        decimal amount,
        string reason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Refunding Stripe payment intent {PaymentIntentId} for amount {Amount}",
            paymentIntentId, amount);

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var service = new RefundService();

            var options = new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId,
                Amount = (long)(amount * 100), // Convert to cents
                Reason = ConvertRefundReason(reason),
                Metadata = new Dictionary<string, string>
                {
                    { "reason", reason }
                }
            };

            var refund = await service.CreateAsync(
                options,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Created Stripe refund {RefundId} with status {Status}",
                refund.Id, refund.Status);

            return refund.Id;
        });
    }

    private static string ConvertRefundReason(string reason)
    {
        // Stripe only accepts specific refund reasons
        if (reason.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
            return "duplicate";
        if (reason.Contains("fraud", StringComparison.OrdinalIgnoreCase))
            return "fraudulent";

        return "requested_by_customer";
    }
}
