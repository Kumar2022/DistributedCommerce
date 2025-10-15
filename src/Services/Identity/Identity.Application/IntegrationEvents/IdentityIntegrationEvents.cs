using BuildingBlocks.EventBus.Abstractions;

namespace Identity.Application.IntegrationEvents;

/// <summary>
/// Integration event published when a new user registers
/// </summary>
public sealed record UserRegisteredIntegrationEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    DateTime RegisteredAt
) : IntegrationEvent(UserId);

/// <summary>
/// Integration event published when a user successfully logs in
/// </summary>
public sealed record UserLoggedInIntegrationEvent(
    Guid UserId,
    string Email,
    string IpAddress,
    string UserAgent,
    DateTime LoggedInAt
) : IntegrationEvent(UserId);

/// <summary>
/// Integration event published when a user's password is changed
/// </summary>
public sealed record PasswordChangedIntegrationEvent(
    Guid UserId,
    string Email,
    DateTime ChangedAt
) : IntegrationEvent(UserId);

/// <summary>
/// Integration event published when a user's profile is updated
/// </summary>
public sealed record UserProfileUpdatedIntegrationEvent(
    Guid UserId,
    string Email,
    Dictionary<string, string> UpdatedFields,
    DateTime UpdatedAt
) : IntegrationEvent(UserId);

/// <summary>
/// Integration event published when a user account is locked
/// </summary>
public sealed record UserAccountLockedIntegrationEvent(
    Guid UserId,
    string Email,
    string Reason,
    DateTime LockedAt
) : IntegrationEvent(UserId);

/// <summary>
/// Integration event published when a user account is unlocked
/// </summary>
public sealed record UserAccountUnlockedIntegrationEvent(
    Guid UserId,
    string Email,
    DateTime UnlockedAt
) : IntegrationEvent(UserId);

/// <summary>
/// Integration event published when a user is deleted/deactivated
/// </summary>
public sealed record UserDeletedIntegrationEvent(
    Guid UserId,
    string Email,
    string Reason,
    DateTime DeletedAt
) : IntegrationEvent(UserId);
