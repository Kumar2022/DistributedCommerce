using Order.Application.DTOs;

namespace Order.Application.Commands;

/// <summary>
/// Handler for ConfirmOrderCommand
/// </summary>
public sealed class ConfirmOrderCommandHandler : ICommandHandler<ConfirmOrderCommand>
{
    private readonly IOrderRepository _orderRepository;

    public ConfirmOrderCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result> Handle(
        ConfirmOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
            return Result.Failure(Error.NotFound("Order", request.OrderId));

        var result = order.Confirm();
        if (result.IsFailure)
            return result;

        await _orderRepository.UpdateAsync(order, cancellationToken);

        return Result.Success();
    }
}
