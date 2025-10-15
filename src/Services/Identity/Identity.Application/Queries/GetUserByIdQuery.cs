using Identity.Application.DTOs;

namespace Identity.Application.Queries;

/// <summary>
/// Query to get a user by ID
/// </summary>
public sealed record GetUserByIdQuery(Guid UserId) : IQuery<UserDto>;
