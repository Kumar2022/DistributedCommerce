using Order.Application.DTOs;

namespace Order.Application.Queries;

/// <summary>
/// Handler for GetOrderByIdQuery
/// </summary>
public sealed class GetOrderByIdQueryHandler : IQueryHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<OrderDto>> Handle(
        GetOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        
        if (order is null)
            return Result.Failure<OrderDto>(Error.NotFound("Order", request.OrderId));

        var orderDto = new OrderDto(
            order.Id,
            order.CustomerId.Value,
            new AddressDto(
                order.ShippingAddress.Street,
                order.ShippingAddress.City,
                order.ShippingAddress.State,
                order.ShippingAddress.PostalCode,
                order.ShippingAddress.Country),
            order.Items.Select(item => new OrderItemDto(
                item.ProductId,
                item.ProductName,
                item.Quantity,
                new MoneyDto(item.UnitPrice.Amount, item.UnitPrice.Currency),
                new MoneyDto(item.TotalPrice.Amount, item.TotalPrice.Currency)
            )).ToList(),
            new MoneyDto(order.TotalAmount.Amount, order.TotalAmount.Currency),
            order.Status,
            order.PaymentId,
            order.TrackingNumber,
            order.CancellationReason,
            order.CreatedAt,
            order.UpdatedAt);

        return Result.Success(orderDto);
    }
}
