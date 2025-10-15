namespace BuildingBlocks.Authentication.Configuration;

/// <summary>
/// JWT configuration options
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";
    
    /// <summary>
    /// Secret key for signing tokens (minimum 32 characters for HS256)
    /// </summary>
    public string Secret { get; set; } = string.Empty;
    
    /// <summary>
    /// Token issuer (typically the Identity service URL)
    /// </summary>
    public string Issuer { get; set; } = "DistributedCommerce";
    
    /// <summary>
    /// Token audience (typically the service consuming the token)
    /// </summary>
    public string Audience { get; set; } = "DistributedCommerce";
    
    /// <summary>
    /// Access token lifetime in minutes (default: 15 minutes)
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    
    /// <summary>
    /// Refresh token lifetime in days (default: 7 days)
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
    
    /// <summary>
    /// Clock skew tolerance in minutes (default: 0)
    /// </summary>
    public int ClockSkewMinutes { get; set; } = 0;
    
    /// <summary>
    /// Validate issuer
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;
    
    /// <summary>
    /// Validate audience
    /// </summary>
    public bool ValidateAudience { get; set; } = true;
    
    /// <summary>
    /// Validate lifetime
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;
    
    /// <summary>
    /// Validate issuer signing key
    /// </summary>
    public bool ValidateIssuerSigningKey { get; set; } = true;
    
    /// <summary>
    /// Require expiration time
    /// </summary>
    public bool RequireExpirationTime { get; set; } = true;
    
    /// <summary>
    /// Require signed tokens
    /// </summary>
    public bool RequireSignedTokens { get; set; } = true;
}
