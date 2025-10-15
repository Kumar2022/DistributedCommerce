using Order.Application.DTOs;

namespace Order.Application.Commands;

/// <summary>
/// Handler for CancelOrderCommand
/// </summary>
public sealed class CancelOrderCommandHandler : ICommandHandler<CancelOrderCommand>
{
    private readonly IOrderRepository _orderRepository;

    public CancelOrderCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result> Handle(
        CancelOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
            return Result.Failure(Error.NotFound("Order", request.OrderId));

        var result = order.Cancel(request.Reason);
        if (result.IsFailure)
            return result;

        await _orderRepository.UpdateAsync(order, cancellationToken);

        return Result.Success();
    }
}
