using Payment.Application.DTOs;

namespace Payment.Application.Queries;

/// <summary>
/// Query to get a payment by ID
/// </summary>
public sealed record GetPaymentByIdQuery(Guid PaymentId) : IQuery<PaymentDto>;
