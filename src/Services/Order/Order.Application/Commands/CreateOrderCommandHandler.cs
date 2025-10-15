using Order.Application.DTOs;
using Order.Application.Sagas;
using Order.Domain.Aggregates.OrderAggregate;
using Order.Domain.ValueObjects;

namespace Order.Application.Commands;

/// <summary>
/// Handler for CreateOrderCommand
/// </summary>
public sealed class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository _orderRepository;
    private readonly OrderCreationSaga _orderCreationSaga;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        OrderCreationSaga orderCreationSaga)
    {
        _orderRepository = orderRepository;
        _orderCreationSaga = orderCreationSaga;
    }

    public async Task<Result<Guid>> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        // Create CustomerId
        var customerId = CustomerId.Create(request.CustomerId);

        // Create Address
        var addressResult = Address.Create(
            request.Street,
            request.City,
            request.State,
            request.PostalCode,
            request.Country);

        if (addressResult.IsFailure)
            return Result.Failure<Guid>(addressResult.Error);

        // Create OrderItems
        var items = new List<OrderItem>();
        foreach (var itemDto in request.Items)
        {
            var moneyResult = Money.Create(itemDto.UnitPrice, itemDto.Currency);
            if (moneyResult.IsFailure)
                return Result.Failure<Guid>(moneyResult.Error);

            var orderItemResult = OrderItem.Create(
                itemDto.ProductId,
                itemDto.ProductName,
                itemDto.Quantity,
                moneyResult.Value);

            if (orderItemResult.IsFailure)
                return Result.Failure<Guid>(orderItemResult.Error);

            items.Add(orderItemResult.Value);
        }

        // Create Order aggregate
        var orderResult = Domain.Aggregates.OrderAggregate.Order.Create(
            customerId,
            addressResult.Value,
            items);

        if (orderResult.IsFailure)
            return Result.Failure<Guid>(orderResult.Error);

        // Save to repository
        await _orderRepository.AddAsync(orderResult.Value, cancellationToken);

        // Start the saga to orchestrate: Inventory Reservation → Payment → Confirmation
        var sagaState = new OrderCreationSagaState
        {
            CorrelationId = Guid.NewGuid(),
            OrderId = orderResult.Value.Id,
            CustomerId = request.CustomerId,
            OrderItems = items.Select(i => new OrderItemSagaData
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice.Amount,
                Currency = i.UnitPrice.Currency
            }).ToList(),
            TotalAmount = orderResult.Value.TotalAmount.Amount,
            Currency = orderResult.Value.TotalAmount.Currency,
            ShippingAddress = new AddressSagaData
            {
                Street = addressResult.Value.Street,
                City = addressResult.Value.City,
                State = addressResult.Value.State,
                PostalCode = addressResult.Value.PostalCode,
                Country = addressResult.Value.Country
            }
        };

        // Execute saga asynchronously (fire and forget)
        // The saga will handle inventory reservation, payment processing, and order confirmation
        _ = Task.Run(async () =>
        {
            try
            {
                await _orderCreationSaga.ExecuteAsync(sagaState, CancellationToken.None);
            }
            catch (Exception)
            {
                // Saga execution errors are logged within the saga
                // The saga will automatically compensate on failure
            }
        }, cancellationToken);

        return Result.Success(orderResult.Value.Id);
    }
}
