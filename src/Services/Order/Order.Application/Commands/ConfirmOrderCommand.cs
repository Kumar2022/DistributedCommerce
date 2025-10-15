namespace Order.Application.Commands;

/// <summary>
/// Command to confirm an order after payment
/// </summary>
public sealed record ConfirmOrderCommand(Guid OrderId) : ICommand;
