namespace BuildingBlocks.Authentication.Models;

/// <summary>
/// JWT token response
/// </summary>
public class TokenResponse
{
    /// <summary>
    /// Access token (short-lived, typically 15 minutes)
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Refresh token (long-lived, typically 7 days)
    /// </summary>
    public string? RefreshToken { get; set; }
    
    /// <summary>
    /// Token type (always "Bearer")
    /// </summary>
    public string TokenType { get; set; } = "Bearer";
    
    /// <summary>
    /// Token expiration in seconds
    /// </summary>
    public int ExpiresIn { get; set; }
    
    /// <summary>
    /// Timestamp when token was issued
    /// </summary>
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Scopes granted to this token
    /// </summary>
    public List<string> Scopes { get; set; } = [];
}

/// <summary>
/// User claims from JWT token
/// </summary>
public class UserClaims
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Username/Email
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// User roles
    /// </summary>
    public List<string> Roles { get; set; } = new();
    
    /// <summary>
    /// Additional custom claims
    /// </summary>
    public Dictionary<string, string> CustomClaims { get; set; } = new();
    
    /// <summary>
    /// Token expiration
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// Check if user has a specific role
    /// </summary>
    public bool HasRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    
    /// <summary>
    /// Check if user has any of the specified roles
    /// </summary>
    public bool HasAnyRole(params string[] roles) => 
        roles.Any(role => Roles.Contains(role, StringComparer.OrdinalIgnoreCase));
    
    /// <summary>
    /// Check if user has all of the specified roles
    /// </summary>
    public bool HasAllRoles(params string[] roles) => 
        roles.All(role => Roles.Contains(role, StringComparer.OrdinalIgnoreCase));
}

/// <summary>
/// Service account credentials for inter-service communication
/// </summary>
public class ServiceAccountCredentials
{
    /// <summary>
    /// Service name (e.g., "order-service")
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;
    
    /// <summary>
    /// API key for service authentication
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Service roles/permissions
    /// </summary>
    public List<string> Permissions { get; set; } = new();
}
