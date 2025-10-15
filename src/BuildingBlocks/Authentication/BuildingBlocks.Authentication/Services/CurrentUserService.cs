using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using AuthClaims = BuildingBlocks.Authentication.Authorization.ClaimTypes;

namespace BuildingBlocks.Authentication.Services;

/// <summary>
/// Default implementation of ICurrentUserService
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Lazy<ClaimsPrincipal?> _principal;
    
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _principal = new Lazy<ClaimsPrincipal?>(() => _httpContextAccessor.HttpContext?.User);
    }
    
    private ClaimsPrincipal? Principal => _principal.Value;
    
    public Guid? UserId
    {
        get
        {
            var userIdClaim = GetClaimValue(ClaimTypes.NameIdentifier) 
                           ?? GetClaimValue(AuthClaims.UserId)
                           ?? GetClaimValue("sub");
            
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }
    
    public string? Username => 
        GetClaimValue(ClaimTypes.Name) 
        ?? GetClaimValue(AuthClaims.Username)
        ?? GetClaimValue("unique_name");
    
    public string? Email => 
        GetClaimValue(ClaimTypes.Email)
        ?? GetClaimValue(AuthClaims.Email);
    
    public IReadOnlyList<string> Roles
    {
        get
        {
            if (Principal == null) return [];
            
            return Principal.Claims
                .Where(c => c.Type is ClaimTypes.Role or AuthClaims.Role)
                .Select(c => c.Value)
                .ToList()
                .AsReadOnly();
        }
    }
    
    public IReadOnlyList<string> Permissions
    {
        get
        {
            if (Principal == null) return [];
            
            return Principal.Claims
                .Where(c => c.Type is AuthClaims.Permission or "permission")
                .Select(c => c.Value)
                .ToList()
                .AsReadOnly();
        }
    }
    
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;
    
    public bool IsInRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role) || !IsAuthenticated)
            return false;
        
        return Roles.Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
    }
    
    public bool HasPermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission) || !IsAuthenticated)
            return false;
        
        return Permissions.Any(p => p.Equals(permission, StringComparison.OrdinalIgnoreCase));
    }
    
    public bool IsInAnyRole(params string[]? roles)
    {
        if (roles == null || roles.Length == 0 || !IsAuthenticated)
            return false;
        
        return roles.Any(IsInRole);
    }
    
    public bool HasAllPermissions(params string[]? permissions)
    {
        if (permissions == null || permissions.Length == 0 || !IsAuthenticated)
            return false;
        
        return permissions.All(HasPermission);
    }
    
    public string? GetClaimValue(string claimType)
    {
        if (string.IsNullOrWhiteSpace(claimType) || Principal == null)
            return null;
        
        return Principal.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
    }
    
    public IReadOnlyList<string> GetClaimValues(string claimType)
    {
        if (string.IsNullOrWhiteSpace(claimType) || Principal == null)
            return [];
        
        return Principal.Claims
            .Where(c => c.Type == claimType)
            .Select(c => c.Value)
            .ToList()
            .AsReadOnly();
    }
    
    public bool IsServiceAccount
    {
        get
        {
            var serviceAccountClaim = GetClaimValue(AuthClaims.ServiceAccount) 
                                   ?? GetClaimValue("service_account");
            
            return bool.TryParse(serviceAccountClaim, out var isServiceAccount) && isServiceAccount;
        }
    }
    
    public string? CorrelationId => 
        GetClaimValue(AuthClaims.CorrelationId)
        ?? _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-ID"].FirstOrDefault()
        ?? _httpContextAccessor.HttpContext?.TraceIdentifier;
}
