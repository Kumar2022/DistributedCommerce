using System.Diagnostics;

namespace ApiGateway.Middleware;

/// <summary>
/// Middleware that measures and logs request performance metrics
/// </summary>
public class PerformanceMonitoringMiddleware(
    RequestDelegate next,
    ILogger<PerformanceMonitoringMiddleware> logger)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path;
        var method = context.Request.Method;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            var statusCode = context.Response.StatusCode;

            // Log slow requests (> 1 second)
            if (elapsedMs > 1000)
            {
                _logger.LogWarning(
                    "Slow request detected: {Method} {Path} completed in {ElapsedMs}ms with status {StatusCode}",
                    method, path, elapsedMs, statusCode);
            }
            else
            {
                _logger.LogInformation(
                    "Request: {Method} {Path} completed in {ElapsedMs}ms with status {StatusCode}",
                    method, path, elapsedMs, statusCode);
            }

            // Add performance header to response
            context.Response.Headers.Append("X-Response-Time-Ms", elapsedMs.ToString());
        }
    }
}

public static class PerformanceMonitoringMiddlewareExtensions
{
    public static IApplicationBuilder UsePerformanceMonitoring(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PerformanceMonitoringMiddleware>();
    }
}
