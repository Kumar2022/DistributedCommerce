namespace Payment.Application.Commands;

/// <summary>
/// Command to process a payment with external provider (Stripe)
/// </summary>
public sealed record ProcessPaymentCommand(
    Guid PaymentId,
    string PaymentMethodId) : ICommand;
