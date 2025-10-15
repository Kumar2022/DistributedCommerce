namespace Order.Application.Commands;

/// <summary>
/// Command to ship an order
/// </summary>
public sealed record ShipOrderCommand(
    Guid OrderId,
    string TrackingNumber,
    string Carrier) : ICommand;
