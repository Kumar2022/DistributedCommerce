using Order.Application.DTOs;

namespace Order.Application.Commands;

/// <summary>
/// Handler for InitiatePaymentCommand
/// </summary>
public sealed class InitiatePaymentCommandHandler : ICommandHandler<InitiatePaymentCommand>
{
    private readonly IOrderRepository _orderRepository;

    public InitiatePaymentCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result> Handle(
        InitiatePaymentCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
            return Result.Failure(Error.NotFound("Order", request.OrderId));

        var result = order.InitiatePayment(request.PaymentMethod);
        if (result.IsFailure)
            return result;

        await _orderRepository.UpdateAsync(order, cancellationToken);

        return Result.Success();
    }
}
