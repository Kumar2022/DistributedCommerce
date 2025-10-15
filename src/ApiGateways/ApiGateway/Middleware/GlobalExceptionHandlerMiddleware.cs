using System.Text.Json;

namespace ApiGateway.Middleware;

/// <summary>
/// Global exception handling middleware
/// </summary>
public class GlobalExceptionHandlerMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlerMiddleware> logger,
    IHostEnvironment environment)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IHostEnvironment _environment = environment ?? throw new ArgumentNullException(nameof(environment));

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;
        var correlationId = context.Items["X-Correlation-ID"]?.ToString() ?? traceId;

        _logger.LogError(exception, 
            "Unhandled exception occurred. TraceId: {TraceId}, CorrelationId: {CorrelationId}", 
            traceId, correlationId);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var errorResponse = new ErrorResponse
        {
            TraceId = traceId,
            CorrelationId = correlationId,
            Error = "An unexpected error occurred",
            Message = _environment.IsDevelopment() 
                ? exception.Message 
                : "Internal server error. Please contact support.",
            Details = _environment.IsDevelopment() 
                ? exception.StackTrace 
                : null,
            Timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        });

        await context.Response.WriteAsync(json);
    }

    private class ErrorResponse
    {
        public string TraceId { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
