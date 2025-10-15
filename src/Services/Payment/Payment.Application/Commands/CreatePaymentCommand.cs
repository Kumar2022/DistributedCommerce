namespace Payment.Application.Commands;

/// <summary>
/// Command to create a new payment
/// </summary>
public sealed record CreatePaymentCommand(
    Guid OrderId,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    string? IdempotencyKey = null) : ICommand<Guid>;
