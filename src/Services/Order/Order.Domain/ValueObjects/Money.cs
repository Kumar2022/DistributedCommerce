namespace Order.Domain.ValueObjects;

/// <summary>
/// Money value object representing an amount in a specific currency
/// </summary>
public sealed class Money : ValueObject
{
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }
    public string Currency { get; }

    public static Result<Money> Create(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            return Result.Failure<Money>(Error.Validation(nameof(Money), "Amount cannot be negative"));

        if (string.IsNullOrWhiteSpace(currency))
            return Result.Failure<Money>(Error.Validation(nameof(Currency), "Currency is required"));

        if (currency.Length != 3)
            return Result.Failure<Money>(Error.Validation(nameof(Currency), "Currency must be 3 characters (ISO 4217)"));

        return Result.Success(new Money(amount, currency.ToUpperInvariant()));
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add {other.Currency} to {Currency}");

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot subtract {other.Currency} from {Currency}");

        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal multiplier)
    {
        return new Money(Amount * multiplier, Currency);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:F2} {Currency}";

    public static Money Zero(string currency = "USD") => new Money(0, currency);
}
