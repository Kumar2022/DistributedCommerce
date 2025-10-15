using Identity.Application.Services;
using Identity.Domain.Aggregates.UserAggregate;
using BuildingBlocks.Authentication.Jwt;
using BuildingBlocks.Authentication.Models;
using System.Security.Claims;

namespace Identity.Infrastructure.Security;

/// <summary>
/// JWT token generator implementation using BuildingBlocks.Authentication
/// </summary>
public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IJwtTokenService _jwtTokenService;

    public JwtTokenGenerator(IJwtTokenService jwtTokenService)
    {
        _jwtTokenService = jwtTokenService;
    }

    public string GenerateToken(User user)
    {
        var userClaims = new UserClaims
        {
            UserId = user.Id, // user.Id is already Guid
            Username = user.Email.Value,
            Roles = new List<string> { "Customer" }, // Default role, can be extended
            CustomClaims = new Dictionary<string, string>
            {
                [ClaimTypes.Email] = user.Email.Value,
                [ClaimTypes.GivenName] = user.FirstName,
                [ClaimTypes.Surname] = user.LastName,
                ["firstName"] = user.FirstName,
                ["lastName"] = user.LastName
            }
        };

        var tokenResponse = _jwtTokenService.GenerateToken(userClaims);
        return tokenResponse.AccessToken;
    }
}
