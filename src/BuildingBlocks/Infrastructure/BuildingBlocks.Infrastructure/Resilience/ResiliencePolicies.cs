using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace BuildingBlocks.Infrastructure.Resilience;

/// <summary>
/// Pre-configured Polly resilience policies for common scenarios
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Retry policy with exponential backoff
    /// </summary>
    public static AsyncRetryPolicy CreateRetryPolicy(int retryCount = 3)
    {
        return Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, timeSpan, attempt, context) =>
                {
                    // Log retry attempt
                    Console.WriteLine($"Retry {attempt} after {timeSpan.TotalSeconds}s due to: {exception.Message}");
                });
    }

    /// <summary>
    /// Circuit breaker policy
    /// </summary>
    public static AsyncCircuitBreakerPolicy CreateCircuitBreakerPolicy(
        int exceptionsBeforeBreaking = 5,
        int durationOfBreakInSeconds = 30)
    {
        return Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: exceptionsBeforeBreaking,
                durationOfBreak: TimeSpan.FromSeconds(durationOfBreakInSeconds),
                onBreak: (exception, duration) =>
                {
                    Console.WriteLine($"Circuit breaker opened for {duration.TotalSeconds}s due to: {exception.Message}");
                },
                onReset: () =>
                {
                    Console.WriteLine("Circuit breaker reset");
                });
    }
}
