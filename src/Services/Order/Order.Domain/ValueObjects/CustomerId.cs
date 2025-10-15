namespace Order.Domain.ValueObjects;

/// <summary>
/// Customer ID value object
/// </summary>
public sealed class CustomerId : ValueObject
{
    private CustomerId(Guid value)
    {
        Value = value;
    }

    public Guid Value { get; }

    public static CustomerId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Customer ID cannot be empty", nameof(value));

        return new CustomerId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(CustomerId customerId) => customerId.Value;
}
