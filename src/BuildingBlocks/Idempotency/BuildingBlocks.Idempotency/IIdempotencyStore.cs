namespace BuildingBlocks.Idempotency;

/// <summary>
/// Store for tracking processed idempotency keys to ensure exactly-once processing
/// Critical for FAANG-scale systems to prevent duplicate event processing
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// Check if an idempotency key has already been processed
    /// </summary>
    /// <param name="idempotencyKey">Unique key identifying the operation</param>
    /// <param name="cancellationToken"></param>
    /// <returns>True if already processed, false otherwise</returns>
    Task<bool> IsProcessedAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark an idempotency key as processed with optional result
    /// </summary>
    /// <param name="idempotencyKey">Unique key identifying the operation</param>
    /// <param name="result">Optional result to store</param>
    /// <param name="ttl">Time-to-live for the idempotency record (default: 24 hours)</param>
    /// <param name="cancellationToken"></param>
    Task MarkAsProcessedAsync(
        string idempotencyKey, 
        object? result = null, 
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the result of a previously processed operation
    /// </summary>
    /// <typeparam name="TResult">Type of the result</typeparam>
    /// <param name="idempotencyKey">Unique key identifying the operation</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The stored result, or null if not found</returns>
    Task<TResult?> GetResultAsync<TResult>(
        string idempotencyKey, 
        CancellationToken cancellationToken = default) where TResult : class;
}
