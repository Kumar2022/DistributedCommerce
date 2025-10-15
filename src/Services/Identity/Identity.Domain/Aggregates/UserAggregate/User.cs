using Identity.Domain.Events;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Aggregates.UserAggregate;

/// <summary>
/// User aggregate root
/// </summary>
public sealed class User : AggregateRoot<Guid>
{
    private User() { } // For EF Core

    public Email Email { get; private set; } = null!;
    public HashedPassword Password { get; private set; } = null!;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    /// <summary>
    /// Create a new user
    /// </summary>
    public static Result<User> Create(
        Email email,
        string plainTextPassword,
        string firstName,
        string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            return Result.Failure<User>(Error.Validation(nameof(FirstName), "First name is required"));

        if (string.IsNullOrWhiteSpace(lastName))
            return Result.Failure<User>(Error.Validation(nameof(LastName), "Last name is required"));

        if (plainTextPassword.Length < 8)
            return Result.Failure<User>(Error.Validation(nameof(Password), "Password must be at least 8 characters"));

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Password = HashedPassword.Create(plainTextPassword),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        user.AddDomainEvent(new UserRegisteredEvent(
            user.Id,
            email.Value,
            user.CreatedAt));

        return Result.Success(user);
    }

    /// <summary>
    /// Authenticate user with password
    /// </summary>
    public Result Login(string plainTextPassword)
    {
        if (!IsActive)
            return Result.Failure(Error.Validation(nameof(User), "User account is not active"));

        if (!Password.Verify(plainTextPassword))
            return Result.Failure(Error.Unauthorized("Invalid password"));

        LastLoginAt = DateTime.UtcNow;
        MarkAsUpdated();

        AddDomainEvent(new UserLoggedInEvent(
            Id,
            Email.Value,
            LastLoginAt.Value));

        return Result.Success();
    }

    /// <summary>
    /// Change user password
    /// </summary>
    public Result ChangePassword(string currentPassword, string newPassword)
    {
        if (!Password.Verify(currentPassword))
            return Result.Failure(Error.Unauthorized("Current password is incorrect"));

        if (newPassword.Length < 8)
            return Result.Failure(Error.Validation(nameof(Password), "Password must be at least 8 characters"));

        Password = HashedPassword.Create(newPassword);
        MarkAsUpdated();

        AddDomainEvent(new PasswordChangedEvent(Id, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Deactivate user account
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    /// <summary>
    /// Activate user account
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }
}
