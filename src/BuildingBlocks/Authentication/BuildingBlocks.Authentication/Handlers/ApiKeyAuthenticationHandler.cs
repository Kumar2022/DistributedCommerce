using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AuthClaims = BuildingBlocks.Authentication.Authorization.ClaimTypes;

namespace BuildingBlocks.Authentication.Handlers;

/// <summary>
/// Options for API key authentication
/// </summary>
public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public string Scheme => DefaultScheme;
    public string HeaderName { get; set; } = "X-API-Key";
    
    /// <summary>
    /// Dictionary of API keys to service account details
    /// In production, load from secure storage (Vault, Key Vault, etc.)
    /// </summary>
    public Dictionary<string, ServiceAccountInfo> ApiKeys { get; set; } = new();
}

/// <summary>
/// Service account information
/// </summary>
public class ServiceAccountInfo
{
    public string ServiceName { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
    public List<string> Roles { get; set; } = new();
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Authentication handler for API key-based authentication
/// Used for service-to-service authentication
/// </summary>
public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if API key header exists
        if (!Request.Headers.TryGetValue(Options.HeaderName, out var apiKeyHeaderValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
        
        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // Validate API key
        if (!Options.ApiKeys.TryGetValue(providedApiKey, out var serviceAccountInfo))
        {
            Logger.LogWarning("Invalid API key provided");
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
        }

        if (!serviceAccountInfo.IsActive)
        {
            Logger.LogWarning("Inactive service account attempted to authenticate: {ServiceName}", 
                serviceAccountInfo.ServiceName);
            return Task.FromResult(AuthenticateResult.Fail("Service account is inactive"));
        }

        // Create claims for the service account
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, serviceAccountInfo.ServiceName),
            new(ClaimTypes.Name, serviceAccountInfo.ServiceName),
            new(AuthClaims.ServiceAccount, "true"),
            new(AuthClaims.ApiKey, providedApiKey)
        };

        // Add roles
        claims.AddRange(serviceAccountInfo.Roles.Select(role => 
            new Claim(ClaimTypes.Role, role)));

        // Add permissions
        claims.AddRange(serviceAccountInfo.Permissions.Select(permission => 
            new Claim(AuthClaims.Permission, permission)));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        Logger.LogInformation("Service account authenticated: {ServiceName}", 
            serviceAccountInfo.ServiceName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = $"{Options.Scheme} realm=\"{Options.HeaderName}\"";
        return base.HandleChallengeAsync(properties);
    }
}
