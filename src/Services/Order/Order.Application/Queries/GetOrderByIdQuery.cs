using Order.Application.DTOs;

namespace Order.Application.Queries;

/// <summary>
/// Query to get an order by ID
/// </summary>
public sealed record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderDto>;
