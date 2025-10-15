using Payment.Application.DTOs;

namespace Payment.Application.Queries;

/// <summary>
/// Query to get payments by order ID
/// </summary>
public sealed record GetPaymentsByOrderIdQuery(Guid OrderId) : IQuery<List<PaymentDto>>;
