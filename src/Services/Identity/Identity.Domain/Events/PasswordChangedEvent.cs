namespace Identity.Domain.Events;

/// <summary>
/// Event raised when a user's password is changed
/// </summary>
public sealed record PasswordChangedEvent(
    Guid UserId,
    DateTime ChangedAt) : DomainEvent;
