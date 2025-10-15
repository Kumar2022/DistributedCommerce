namespace Identity.Domain.Events;

/// <summary>
/// Event raised when a new user registers
/// </summary>
public sealed record UserRegisteredEvent(
    Guid UserId,
    string Email,
    DateTime RegisteredAt) : DomainEvent;
