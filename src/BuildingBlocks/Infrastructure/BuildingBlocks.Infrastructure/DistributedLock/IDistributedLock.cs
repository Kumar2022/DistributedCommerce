namespace BuildingBlocks.Infrastructure.DistributedLock;

/// <summary>
/// Distributed lock interface for preventing concurrent saga execution
/// Critical for FAANG-scale systems to prevent race conditions across multiple instances
/// </summary>
public interface IDistributedLock
{
    /// <summary>
    /// Acquire a distributed lock
    /// </summary>
    /// <param name="key">Unique key identifying the resource to lock</param>
    /// <param name="expirationTime">Lock expiration time (default: 30 seconds)</param>
    /// <param name="waitTime">Maximum time to wait for lock acquisition (default: 10 seconds)</param>
    /// <param name="retryTime">Time between retry attempts (default: 100ms)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Lock handle that must be disposed to release the lock, or null if lock couldn't be acquired</returns>
    Task<IDisposable?> AcquireAsync(
        string key,
        TimeSpan? expirationTime = null,
        TimeSpan? waitTime = null,
        TimeSpan? retryTime = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Try to acquire a lock without waiting
    /// </summary>
    /// <param name="key">Unique key identifying the resource to lock</param>
    /// <param name="expirationTime">Lock expiration time (default: 30 seconds)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Lock handle that must be disposed to release the lock, or null if lock couldn't be acquired</returns>
    Task<IDisposable?> TryAcquireAsync(
        string key,
        TimeSpan? expirationTime = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a lock is currently held
    /// </summary>
    /// <param name="key">Unique key identifying the resource</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if lock is held, false otherwise</returns>
    Task<bool> IsLockedAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extend the expiration of an existing lock
    /// </summary>
    /// <param name="key">Unique key identifying the resource</param>
    /// <param name="expirationTime">New expiration time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if extension successful, false otherwise</returns>
    Task<bool> ExtendLockAsync(
        string key,
        TimeSpan expirationTime,
        CancellationToken cancellationToken = default);
}
