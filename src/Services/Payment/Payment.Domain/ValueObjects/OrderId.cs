using BuildingBlocks.Domain.ValueObjects;

namespace Payment.Domain.ValueObjects;

/// <summary>
/// Order ID value object for strong typing
/// </summary>
public sealed class OrderId : ValueObject
{
    public Guid Value { get; }

    private OrderId(Guid value)
    {
        Value = value;
    }

    public static OrderId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Order ID cannot be empty", nameof(value));

        return new OrderId(value);
    }

    public static implicit operator Guid(OrderId orderId) => orderId.Value;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
