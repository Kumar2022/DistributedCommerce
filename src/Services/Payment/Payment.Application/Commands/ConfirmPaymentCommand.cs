namespace Payment.Application.Commands;

/// <summary>
/// Command to confirm a payment succeeded (webhook handler)
/// </summary>
public sealed record ConfirmPaymentCommand(
    string ExternalPaymentId) : ICommand;
