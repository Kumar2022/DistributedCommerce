using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace BuildingBlocks.Resilience;

/// <summary>
/// Extension methods for adding resilience patterns (Circuit Breaker, Retry, Timeout)
/// FAANG-scale resilience patterns for distributed systems
/// </summary>
public static class ResilienceExtensions
{
    /// <summary>
    /// Adds standard resilience policies to HTTP clients
    /// Includes: Retry with exponential backoff, Circuit Breaker, Timeout
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddResiliencePolicies(this IServiceCollection services)
    {
        // Default resilient HTTP client
        services.AddHttpClient("resilient")
            .AddStandardResilienceHandler(options =>
            {
                // Configure retry: 3 attempts with exponential backoff
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                options.Retry.UseJitter = true;
                options.Retry.Delay = TimeSpan.FromSeconds(1);
                
                // Configure circuit breaker: Opens at 50% failure rate
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.MinimumThroughput = 10;
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
                
                // Configure timeout: 30 seconds total
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
            });

        // Aggressive resilient HTTP client for critical operations
        services.AddHttpClient("resilient-aggressive")
            .AddStandardResilienceHandler(options =>
            {
                // More aggressive retry: 5 attempts
                options.Retry.MaxRetryAttempts = 5;
                options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                options.Retry.UseJitter = true;
                options.Retry.Delay = TimeSpan.FromMilliseconds(500);
                
                // More sensitive circuit breaker: Opens at 30% failure rate
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
                options.CircuitBreaker.FailureRatio = 0.3;
                options.CircuitBreaker.MinimumThroughput = 5;
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
                
                // Shorter timeouts
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(15);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
            });

        // Conservative resilient HTTP client for non-critical operations
        services.AddHttpClient("resilient-conservative")
            .AddStandardResilienceHandler(options =>
            {
                // Minimal retry: 2 attempts
                options.Retry.MaxRetryAttempts = 2;
                options.Retry.BackoffType = Polly.DelayBackoffType.Linear;
                options.Retry.Delay = TimeSpan.FromSeconds(2);
                
                // Lenient circuit breaker: Opens at 70% failure rate
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
                options.CircuitBreaker.FailureRatio = 0.7;
                options.CircuitBreaker.MinimumThroughput = 20;
                options.CircuitBreaker.BreakDuration = TimeSpan.FromMinutes(1);
                
                // Longer timeouts
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(20);
            });

        return services;
    }

    /// <summary>
    /// Adds resilience policies to a specific named HTTP client
    /// </summary>
    /// <param name="builder">The HTTP client builder</param>
    /// <param name="maxRetryAttempts">Maximum retry attempts</param>
    /// <param name="circuitBreakerFailureRatio">Circuit breaker failure ratio threshold (0.0-1.0)</param>
    /// <param name="timeoutSeconds">Total timeout in seconds</param>
    /// <returns>The HTTP resilience pipeline builder for chaining</returns>
    public static IHttpStandardResiliencePipelineBuilder AddCustomResilienceHandler(
        this IHttpClientBuilder builder,
        int maxRetryAttempts = 3,
        double circuitBreakerFailureRatio = 0.5,
        int timeoutSeconds = 30)
    {
        return builder.AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = maxRetryAttempts;
            options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;
            
            options.CircuitBreaker.FailureRatio = circuitBreakerFailureRatio;
            options.CircuitBreaker.MinimumThroughput = 10;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
            
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(timeoutSeconds / 3);
        });
    }
}
