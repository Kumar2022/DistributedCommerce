using BuildingBlocks.Domain.ValueObjects;

namespace Payment.Domain.ValueObjects;

/// <summary>
/// Money value object for payment amounts
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> Create(decimal amount, string currency)
    {
        if (amount < 0)
            return Result.Failure<Money>(Error.Validation(
                "Amount",
                "Amount cannot be negative"));

        if (string.IsNullOrWhiteSpace(currency))
            return Result.Failure<Money>(Error.Validation(
                "Currency",
                "Currency is required"));

        if (currency.Length != 3)
            return Result.Failure<Money>(Error.Validation(
                "Currency",
                "Currency must be 3 characters (ISO 4217)"));

        return Result.Success(new Money(amount, currency.ToUpperInvariant()));
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException(
                $"Cannot add different currencies: {Currency} and {other.Currency}");

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException(
                $"Cannot subtract different currencies: {Currency} and {other.Currency}");

        return new Money(Amount - other.Amount, Currency);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount} {Currency}";
}
