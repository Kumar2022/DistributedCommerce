namespace Order.Application.Commands;

/// <summary>
/// Command to cancel an order
/// </summary>
public sealed record CancelOrderCommand(
    Guid OrderId,
    string Reason) : ICommand;
