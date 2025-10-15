using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace ApiGateway.Extensions;

/// <summary>
/// Extension methods for configuring rate limiting
/// </summary>
public static class RateLimitingExtensions
{
    public static IServiceCollection AddGatewayRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var rateLimitConfig = configuration.GetSection("RateLimiting");

        services.AddRateLimiter(options =>
        {
            // Fixed Window Limiter - Good for API quotas
            options.AddFixedWindowLimiter("fixed-window", limiterOptions =>
            {
                limiterOptions.PermitLimit = rateLimitConfig.GetValue<int>("FixedWindow:PermitLimit");
                limiterOptions.Window = rateLimitConfig.GetValue<TimeSpan>("FixedWindow:Window");
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = rateLimitConfig.GetValue<int>("FixedWindow:QueueLimit");
            });

            // Token Bucket Limiter - Good for burst handling
            options.AddTokenBucketLimiter("token-bucket", limiterOptions =>
            {
                limiterOptions.TokenLimit = rateLimitConfig.GetValue<int>("TokenBucket:TokenLimit");
                limiterOptions.ReplenishmentPeriod = rateLimitConfig.GetValue<TimeSpan>("TokenBucket:ReplenishmentPeriod");
                limiterOptions.TokensPerPeriod = rateLimitConfig.GetValue<int>("TokenBucket:TokensPerPeriod");
                limiterOptions.QueueLimit = rateLimitConfig.GetValue<int>("TokenBucket:QueueLimit");
                limiterOptions.AutoReplenishment = true;
            });

            // Sliding Window Limiter - Smoother rate limiting
            options.AddSlidingWindowLimiter("sliding-window", limiterOptions =>
            {
                limiterOptions.PermitLimit = 100;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.SegmentsPerWindow = 6; // 10-second segments
                limiterOptions.QueueLimit = 10;
            });

            // Concurrency Limiter - Limit concurrent requests
            options.AddConcurrencyLimiter("concurrency", limiterOptions =>
            {
                limiterOptions.PermitLimit = rateLimitConfig.GetValue<int>("Concurrency:PermitLimit");
                limiterOptions.QueueLimit = rateLimitConfig.GetValue<int>("Concurrency:QueueLimit");
            });

            // Per-user rate limiting (partitioned by user ID)
            options.AddPolicy("per-user", context =>
            {
                var userId = context.User?.FindFirst("sub")?.Value ?? "anonymous";
                
                return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 5
                });
            });

            // Global rejection handler
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }

                var response = new
                {
                    error = "Rate limit exceeded",
                    message = "Too many requests. Please try again later.",
                    retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry) ? (int)retry.TotalSeconds : (int?)null
                };

                await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken: cancellationToken);
            };
        });

        return services;
    }
}
