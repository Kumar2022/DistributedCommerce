using BuildingBlocks.Authentication.Middleware;
using Microsoft.AspNetCore.Builder;

namespace BuildingBlocks.Authentication.Extensions;

/// <summary>
/// Extension methods for adding authentication middleware to the pipeline
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Add correlation ID middleware to track requests across services
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
