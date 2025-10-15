using Order.Domain.Enums;

namespace Order.Application.DTOs;

/// <summary>
/// Order read model DTO
/// </summary>
public sealed record OrderDto(
    Guid Id,
    Guid CustomerId,
    AddressDto ShippingAddress,
    List<OrderItemDto> Items,
    MoneyDto TotalAmount,
    OrderStatus Status,
    Guid? PaymentId,
    string? TrackingNumber,
    string? CancellationReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>
/// Address DTO
/// </summary>
public sealed record AddressDto(
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country);

/// <summary>
/// Order item DTO
/// </summary>
public sealed record OrderItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    MoneyDto UnitPrice,
    MoneyDto TotalPrice);

/// <summary>
/// Money DTO
/// </summary>
public sealed record MoneyDto(
    decimal Amount,
    string Currency);
