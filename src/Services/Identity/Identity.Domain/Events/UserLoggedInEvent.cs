namespace Identity.Domain.Events;

/// <summary>
/// Event raised when a user successfully logs in
/// </summary>
public sealed record UserLoggedInEvent(
    Guid UserId,
    string Email,
    DateTime LoggedInAt) : DomainEvent;
