using Order.Application.DTOs;

namespace Order.Application.Commands;

/// <summary>
/// Handler for ShipOrderCommand
/// </summary>
public sealed class ShipOrderCommandHandler : ICommandHandler<ShipOrderCommand>
{
    private readonly IOrderRepository _orderRepository;

    public ShipOrderCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result> Handle(
        ShipOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
            return Result.Failure(Error.NotFound("Order", request.OrderId));

        var result = order.Ship(request.TrackingNumber, request.Carrier);
        if (result.IsFailure)
            return result;

        await _orderRepository.UpdateAsync(order, cancellationToken);

        return Result.Success();
    }
}
