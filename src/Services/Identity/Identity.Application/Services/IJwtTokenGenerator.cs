using Identity.Domain.Aggregates.UserAggregate;

namespace Identity.Application.Services;

/// <summary>
/// Service for generating JWT tokens
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Generate a JWT token for the given user
    /// </summary>
    string GenerateToken(User user);
}
