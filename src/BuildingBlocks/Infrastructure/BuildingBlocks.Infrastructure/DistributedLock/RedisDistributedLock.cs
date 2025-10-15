using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BuildingBlocks.Infrastructure.DistributedLock;

/// <summary>
/// Redis-based distributed lock using RedLock algorithm
/// Provides distributed locking across multiple service instances
/// Ideal for high-throughput scenarios with Redis already in use
/// </summary>
public class RedisDistributedLock(
    IConnectionMultiplexer redis,
    ILogger<RedisDistributedLock> logger)
    : IDistributedLock
{
    private readonly IConnectionMultiplexer _redis = redis ?? throw new ArgumentNullException(nameof(redis));
    private readonly ILogger<RedisDistributedLock> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private static readonly TimeSpan DefaultExpirationTime = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DefaultWaitTime = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan DefaultRetryTime = TimeSpan.FromMilliseconds(100);
    private const string LockKeyPrefix = "distributed-lock:";

    public async Task<IDisposable?> AcquireAsync(
        string key,
        TimeSpan? expirationTime = null,
        TimeSpan? waitTime = null,
        TimeSpan? retryTime = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Lock key cannot be null or empty", nameof(key));

        var expiration = expirationTime ?? DefaultExpirationTime;
        var wait = waitTime ?? DefaultWaitTime;
        var retry = retryTime ?? DefaultRetryTime;

        _logger.LogDebug("Attempting to acquire lock for key {LockKey}", key);

        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < wait)
        {
            var lockHandle = await TryAcquireAsync(key, expiration, cancellationToken);
            if (lockHandle != null)
            {
                _logger.LogInformation(
                    "Successfully acquired lock for key {LockKey} after {ElapsedMs}ms",
                    key,
                    (DateTime.UtcNow - startTime).TotalMilliseconds);
                return lockHandle;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Lock acquisition cancelled for key {LockKey}", key);
                return null;
            }

            await Task.Delay(retry, cancellationToken);
        }

        _logger.LogWarning(
            "Failed to acquire lock for key {LockKey} after {WaitMs}ms",
            key,
            wait.TotalMilliseconds);

        return null;
    }

    public async Task<IDisposable?> TryAcquireAsync(
        string key,
        TimeSpan? expirationTime = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Lock key cannot be null or empty", nameof(key));

        var expiration = expirationTime ?? DefaultExpirationTime;
        var lockKey = GetLockKey(key);
        var lockValue = Guid.NewGuid().ToString(); // Unique value to ensure only lock owner can release

        try
        {
            var db = _redis.GetDatabase();

            // Try to acquire lock using SET NX (set if not exists) with expiration
            var acquired = await db.StringSetAsync(
                lockKey,
                lockValue,
                expiration,
                When.NotExists);

            if (!acquired) return null;
            _logger.LogDebug("Acquired Redis lock for key {LockKey}", key);
            return new RedisLockHandle(db, lockKey, lockValue, key, _logger);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring lock for key {LockKey}", key);
            return null;
        }
    }

    public async Task<bool> IsLockedAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Lock key cannot be null or empty", nameof(key));

        try
        {
            var db = _redis.GetDatabase();
            var lockKey = GetLockKey(key);
            return await db.KeyExistsAsync(lockKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking lock status for key {LockKey}", key);
            return false;
        }
    }

    public async Task<bool> ExtendLockAsync(
        string key,
        TimeSpan expirationTime,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Lock key cannot be null or empty", nameof(key));

        try
        {
            var db = _redis.GetDatabase();
            var lockKey = GetLockKey(key);

            // Extend expiration time if key exists
            return await db.KeyExpireAsync(lockKey, expirationTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extending lock for key {LockKey}", key);
            return false;
        }
    }

    private static string GetLockKey(string key) => $"{LockKeyPrefix}{key}";

    /// <summary>
    /// Lock handle that releases the lock when disposed
    /// </summary>
    private class RedisLockHandle(
        IDatabase database,
        string lockKey,
        string lockValue,
        string key,
        ILogger logger)
        : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                // Release lock only if we still own it (check value matches)
                // Using Lua script for atomic compare-and-delete
                const string script = @"
                    if redis.call('get', KEYS[1]) == ARGV[1] then
                        return redis.call('del', KEYS[1])
                    else
                        return 0
                    end";

                var result = database.ScriptEvaluate(
                    script,
                    [lockKey],
                    [lockValue]);

                if ((int)result == 1)
                {
                    logger.LogDebug("Released Redis lock for key {LockKey}", key);
                }
                else
                {
                    logger.LogWarning(
                        "Failed to release Redis lock for key {LockKey} - lock was already released or expired",
                        key);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error releasing lock for key {LockKey}", key);
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}
