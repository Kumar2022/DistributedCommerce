namespace Identity.Domain.ValueObjects;

/// <summary>
/// Email value object with validation
/// </summary>
public sealed class Email : ValueObject
{
    private Email(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<Email> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<Email>(Error.Validation(nameof(Email), "Email cannot be empty"));

        email = email.Trim().ToLowerInvariant();

        if (email.Length > 255)
            return Result.Failure<Email>(Error.Validation(nameof(Email), "Email is too long"));

        if (!IsValidEmail(email))
            return Result.Failure<Email>(Error.Validation(nameof(Email), "Email format is invalid"));

        return Result.Success(new Email(email));
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
