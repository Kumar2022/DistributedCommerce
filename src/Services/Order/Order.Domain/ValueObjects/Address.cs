namespace Order.Domain.ValueObjects;

/// <summary>
/// Address value object
/// </summary>
public sealed class Address : ValueObject
{
    private Address(
        string street,
        string city,
        string state,
        string postalCode,
        string country)
    {
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }

    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string PostalCode { get; }
    public string Country { get; }

    public static Result<Address> Create(
        string street,
        string city,
        string state,
        string postalCode,
        string country)
    {
        if (string.IsNullOrWhiteSpace(street))
            return Result.Failure<Address>(Error.Validation(nameof(Street), "Street is required"));

        if (string.IsNullOrWhiteSpace(city))
            return Result.Failure<Address>(Error.Validation(nameof(City), "City is required"));

        if (string.IsNullOrWhiteSpace(postalCode))
            return Result.Failure<Address>(Error.Validation(nameof(PostalCode), "Postal code is required"));

        if (string.IsNullOrWhiteSpace(country))
            return Result.Failure<Address>(Error.Validation(nameof(Country), "Country is required"));

        return Result.Success(new Address(
            street.Trim(),
            city.Trim(),
            state?.Trim() ?? string.Empty,
            postalCode.Trim(),
            country.Trim()));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return PostalCode;
        yield return Country;
    }

    public override string ToString() =>
        $"{Street}, {City}, {State} {PostalCode}, {Country}";
}
