namespace BuildingBlocks.Authentication.Services;

/// <summary>
/// Service to access current authenticated user information
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Current user ID
    /// </summary>
    Guid? UserId { get; }
    
    /// <summary>
    /// Current username
    /// </summary>
    string? Username { get; }
    
    /// <summary>
    /// Current user email
    /// </summary>
    string? Email { get; }
    
    /// <summary>
    /// User roles
    /// </summary>
    IReadOnlyList<string> Roles { get; }
    
    /// <summary>
    /// User permissions
    /// </summary>
    IReadOnlyList<string> Permissions { get; }
    
    /// <summary>
    /// Check if user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }
    
    /// <summary>
    /// Check if user is in role
    /// </summary>
    bool IsInRole(string role);
    
    /// <summary>
    /// Check if user has permission
    /// </summary>
    bool HasPermission(string permission);
    
    /// <summary>
    /// Check if user has any of the specified roles
    /// </summary>
    bool IsInAnyRole(params string[] roles);
    
    /// <summary>
    /// Check if user has all of the specified permissions
    /// </summary>
    bool HasAllPermissions(params string[] permissions);
    
    /// <summary>
    /// Get claim value
    /// </summary>
    string? GetClaimValue(string claimType);
    
    /// <summary>
    /// Get all claim values for a claim type
    /// </summary>
    IReadOnlyList<string> GetClaimValues(string claimType);
    
    /// <summary>
    /// Check if this is a service account
    /// </summary>
    bool IsServiceAccount { get; }
    
    /// <summary>
    /// Get correlation ID for the current request
    /// </summary>
    string? CorrelationId { get; }
}
