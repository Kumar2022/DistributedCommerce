using Order.Domain.ValueObjects;

namespace Order.Application.Commands;

/// <summary>
/// Command to create a new order
/// </summary>
public sealed record CreateOrderCommand(
    Guid CustomerId,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country,
    List<CreateOrderItemDto> Items) : ICommand<Guid>;

/// <summary>
/// DTO for order item in create command
/// </summary>
public sealed record CreateOrderItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    string Currency = "USD");
