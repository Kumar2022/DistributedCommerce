namespace Payment.Application.Commands;

/// <summary>
/// Command to refund a payment
/// </summary>
public sealed record RefundPaymentCommand(
    Guid PaymentId,
    decimal RefundAmount,
    string Reason) : ICommand;
