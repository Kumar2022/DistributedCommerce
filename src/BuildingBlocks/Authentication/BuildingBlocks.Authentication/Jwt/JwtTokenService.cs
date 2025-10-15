using BuildingBlocks.Authentication.Configuration;
using BuildingBlocks.Authentication.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace BuildingBlocks.Authentication.Jwt;

/// <summary>
/// JWT token generator and validator
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generate access token
    /// </summary>
    TokenResponse GenerateToken(UserClaims userClaims);
    
    /// <summary>
    /// Generate refresh token
    /// </summary>
    string GenerateRefreshToken();
    
    /// <summary>
    /// Validate token and extract claims
    /// </summary>
    Task<UserClaims?> ValidateTokenAsync(string token);
    
    /// <summary>
    /// Generate service account token
    /// </summary>
    TokenResponse GenerateServiceToken(ServiceAccountCredentials credentials);
}

/// <summary>
/// Default JWT token service implementation
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _jwtOptions;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly TokenValidationParameters _validationParameters;

    public JwtTokenService(JwtOptions jwtOptions)
    {
        _jwtOptions = jwtOptions ?? throw new ArgumentNullException(nameof(jwtOptions));
        _tokenHandler = new JwtSecurityTokenHandler();
        
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = _jwtOptions.ValidateIssuer,
            ValidateAudience = _jwtOptions.ValidateAudience,
            ValidateLifetime = _jwtOptions.ValidateLifetime,
            ValidateIssuerSigningKey = _jwtOptions.ValidateIssuerSigningKey,
            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret)),
            ClockSkew = TimeSpan.FromMinutes(_jwtOptions.ClockSkewMinutes),
            RequireExpirationTime = _jwtOptions.RequireExpirationTime,
            RequireSignedTokens = _jwtOptions.RequireSignedTokens
        };
    }

    public TokenResponse GenerateToken(UserClaims userClaims)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userClaims.UserId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, userClaims.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Add roles
        claims.AddRange(userClaims.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        // Add custom claims
        claims.AddRange(userClaims.CustomClaims.Select(kv => new Claim(kv.Key, kv.Value)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expirationTime = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expirationTime,
            signingCredentials: credentials
        );

        var tokenString = _tokenHandler.WriteToken(token);

        return new TokenResponse
        {
            AccessToken = tokenString,
            TokenType = "Bearer",
            ExpiresIn = _jwtOptions.AccessTokenExpirationMinutes * 60,
            IssuedAt = DateTime.UtcNow,
            Scopes = userClaims.Roles
        };
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public async Task<UserClaims?> ValidateTokenAsync(string token)
    {
        try
        {
            var principal = await _tokenHandler.ValidateTokenAsync(token, _validationParameters);

            if (!principal.IsValid)
                return null;

            var claims = principal.ClaimsIdentity.Claims;
            
            var userIdClaim = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
            var usernameClaim = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.UniqueName);
            
            if (userIdClaim == null || usernameClaim == null)
                return null;

            var userClaims = new UserClaims
            {
                UserId = Guid.Parse(userIdClaim.Value),
                Username = usernameClaim.Value,
                Roles = claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList(),
                CustomClaims = claims
                    .Where(c => c.Type != JwtRegisteredClaimNames.Sub 
                             && c.Type != JwtRegisteredClaimNames.UniqueName 
                             && c.Type != ClaimTypes.Role
                             && c.Type != JwtRegisteredClaimNames.Jti
                             && c.Type != JwtRegisteredClaimNames.Iat
                             && c.Type != JwtRegisteredClaimNames.Exp
                             && c.Type != JwtRegisteredClaimNames.Nbf
                             && c.Type != JwtRegisteredClaimNames.Iss
                             && c.Type != JwtRegisteredClaimNames.Aud)
                    .ToDictionary(c => c.Type, c => c.Value),
                ExpiresAt = principal.SecurityToken.ValidTo
            };

            return userClaims;
        }
        catch
        {
            return null;
        }
    }

    public TokenResponse GenerateServiceToken(ServiceAccountCredentials credentials)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, credentials.ServiceName),
            new(JwtRegisteredClaimNames.UniqueName, credentials.ServiceName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("service_account", "true"),
            new("api_key", credentials.ApiKey)
        };

        // Add permissions as claims
        claims.AddRange(credentials.Permissions.Select(p => new Claim("permission", p)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expirationTime = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expirationTime,
            signingCredentials: signingCredentials
        );

        var tokenString = _tokenHandler.WriteToken(token);

        return new TokenResponse
        {
            AccessToken = tokenString,
            TokenType = "Bearer",
            ExpiresIn = _jwtOptions.AccessTokenExpirationMinutes * 60,
            IssuedAt = DateTime.UtcNow,
            Scopes = credentials.Permissions
        };
    }
}
