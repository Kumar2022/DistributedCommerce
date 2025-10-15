namespace Identity.Application.DTOs;

/// <summary>
/// Data transfer object for User
/// </summary>
public sealed record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool IsActive,
    DateTime? LastLoginAt,
    DateTime CreatedAt);
