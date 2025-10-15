namespace Order.Application.Commands;

/// <summary>
/// Command to initiate payment for an order
/// </summary>
public sealed record InitiatePaymentCommand(
    Guid OrderId,
    string PaymentMethod) : ICommand;
