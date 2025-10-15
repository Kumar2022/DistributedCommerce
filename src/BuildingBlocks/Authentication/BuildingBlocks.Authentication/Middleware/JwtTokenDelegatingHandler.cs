using BuildingBlocks.Authentication.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace BuildingBlocks.Authentication.Middleware;

/// <summary>
/// Delegating handler to add JWT token to outgoing HTTP requests
/// </summary>
public class JwtTokenDelegatingHandler(
    IHttpContextAccessor httpContextAccessor,
    IJwtTokenService jwtTokenService,
    ILogger<JwtTokenDelegatingHandler> logger)
    : DelegatingHandler
{
    private readonly IJwtTokenService _jwtTokenService = jwtTokenService;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Try to get token from current HTTP context
        var httpContext = httpContextAccessor.HttpContext;
        
        if (httpContext != null)
        {
            var authHeader = httpContext.Request.Headers.Authorization.ToString();
            
            if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer "))
            {
                // Propagate the existing token
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authHeader[7..]);
                
                logger.LogDebug("Propagating JWT token to outgoing request: {RequestUri}", request.RequestUri);
            }
        }
        
        // Add correlation ID
        if (httpContext == null) return await base.SendAsync(request, cancellationToken);
        var correlationId = httpContext.Request.Headers["X-Correlation-Id"].ToString();
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            request.Headers.Add("X-Correlation-Id", correlationId);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
