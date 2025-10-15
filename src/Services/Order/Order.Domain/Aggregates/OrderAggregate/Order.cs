using Order.Domain.Enums;
using Order.Domain.Events;
using Order.Domain.ValueObjects;

namespace Order.Domain.Aggregates.OrderAggregate;

/// <summary>
/// Order aggregate root with event sourcing support
/// This aggregate is the core of the Order Service and follows event sourcing principles
/// </summary>
public sealed class Order : AggregateRoot<Guid>
{
    private readonly List<OrderItem> _items = [];

    // For Marten event store reconstruction
    private Order() { }

    public CustomerId CustomerId { get; private set; } = null!;
    public Address ShippingAddress { get; private set; } = null!;
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    public Money TotalAmount { get; private set; } = Money.Zero();
    public OrderStatus Status { get; private set; }
    public Guid? PaymentId { get; private set; }
    public string? TrackingNumber { get; private set; }
    public string? CancellationReason { get; private set; }

    /// <summary>
    /// Create a new order (factory method)
    /// </summary>
    public static Result<Order> Create(
        CustomerId customerId,
        Address shippingAddress,
        List<OrderItem> items)
    {
        // Validation
        if (items == null || items.Count == 0)
            return Result.Failure<Order>(Error.Validation(nameof(Items), "Order must have at least one item"));

        var order = new Order();
        var totalAmount = items.Aggregate(Money.Zero(), (sum, item) => sum.Add(item.TotalPrice));

        // Apply event (this will call the When method)
        order.ApplyEvent(new OrderCreatedEvent(
            Guid.NewGuid(),
            customerId,
            shippingAddress,
            items,
            totalAmount,
            DateTime.UtcNow));

        return Result.Success(order);
    }

    /// <summary>
    /// Add an item to the order
    /// </summary>
    public Result AddItem(OrderItem item)
    {
        if (Status != OrderStatus.Pending)
            return Result.Failure(Error.Conflict("Cannot add items to a non-pending order"));

        ApplyEvent(new OrderItemAddedEvent(Id, item, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Initiate payment for the order
    /// </summary>
    public Result InitiatePayment(string paymentMethod)
    {
        if (Status != OrderStatus.Pending)
            return Result.Failure(Error.Conflict("Cannot initiate payment for a non-pending order"));

        if (_items.Count == 0)
            return Result.Failure(Error.Validation(nameof(Items), "Cannot initiate payment for an empty order"));

        ApplyEvent(new PaymentInitiatedEvent(Id, TotalAmount, paymentMethod, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Complete payment for the order
    /// </summary>
    public Result CompletePayment(Guid paymentId)
    {
        if (Status != OrderStatus.PaymentInitiated)
            return Result.Failure(Error.Conflict("Order payment has not been initiated"));

        ApplyEvent(new PaymentCompletedEvent(Id, paymentId, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Confirm the order
    /// </summary>
    public Result Confirm()
    {
        if (Status != OrderStatus.PaymentCompleted)
            return Result.Failure(Error.Conflict("Cannot confirm order without completed payment"));

        ApplyEvent(new OrderConfirmedEvent(Id, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Mark order as shipped
    /// </summary>
    public Result Ship(string trackingNumber, string carrier)
    {
        if (Status != OrderStatus.Confirmed)
            return Result.Failure(Error.Conflict("Cannot ship a non-confirmed order"));

        if (string.IsNullOrWhiteSpace(trackingNumber))
            return Result.Failure(Error.Validation(nameof(TrackingNumber), "Tracking number is required"));

        ApplyEvent(new OrderShippedEvent(Id, trackingNumber, carrier, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Cancel the order
    /// </summary>
    public Result Cancel(string reason)
    {
        if (Status == OrderStatus.Shipped || Status == OrderStatus.Delivered)
            return Result.Failure(Error.Conflict("Cannot cancel a shipped or delivered order"));

        if (Status == OrderStatus.Cancelled)
            return Result.Failure(Error.Conflict("Order is already cancelled"));

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(Error.Validation(nameof(CancellationReason), "Cancellation reason is required"));

        ApplyEvent(new OrderCancelledEvent(Id, reason, DateTime.UtcNow));

        return Result.Success();
    }

    #region Event Handlers (When methods for Event Sourcing)

    /// <summary>
    /// Apply OrderCreatedEvent to update state
    /// </summary>
    public void When(OrderCreatedEvent evt)
    {
        Id = evt.OrderId;
        CustomerId = evt.CustomerId;
        ShippingAddress = evt.ShippingAddress;
        _items.AddRange(evt.Items);
        TotalAmount = evt.TotalAmount;
        Status = OrderStatus.Pending;
        CreatedAt = evt.CreatedAt;
    }

    /// <summary>
    /// Apply OrderItemAddedEvent to update state
    /// </summary>
    public void When(OrderItemAddedEvent evt)
    {
        _items.Add(evt.Item);
        TotalAmount = _items.Aggregate(Money.Zero(), (sum, item) => sum.Add(item.TotalPrice));
        MarkAsUpdated();
    }

    /// <summary>
    /// Apply PaymentInitiatedEvent to update state
    /// </summary>
    public void When(PaymentInitiatedEvent evt)
    {
        Status = OrderStatus.PaymentInitiated;
        MarkAsUpdated();
    }

    /// <summary>
    /// Apply PaymentCompletedEvent to update state
    /// </summary>
    public void When(PaymentCompletedEvent evt)
    {
        Status = OrderStatus.PaymentCompleted;
        PaymentId = evt.PaymentId;
        MarkAsUpdated();
    }

    /// <summary>
    /// Apply OrderConfirmedEvent to update state
    /// </summary>
    public void When(OrderConfirmedEvent evt)
    {
        Status = OrderStatus.Confirmed;
        MarkAsUpdated();
    }

    /// <summary>
    /// Apply OrderShippedEvent to update state
    /// </summary>
    public void When(OrderShippedEvent evt)
    {
        Status = OrderStatus.Shipped;
        TrackingNumber = evt.TrackingNumber;
        MarkAsUpdated();
    }

    /// <summary>
    /// Apply OrderCancelledEvent to update state
    /// </summary>
    public void When(OrderCancelledEvent evt)
    {
        Status = OrderStatus.Cancelled;
        CancellationReason = evt.Reason;
        MarkAsUpdated();
    }

    #endregion
}
