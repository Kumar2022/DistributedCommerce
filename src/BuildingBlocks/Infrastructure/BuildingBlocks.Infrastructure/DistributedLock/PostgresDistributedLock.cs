using Microsoft.Extensions.Logging;
using Npgsql;

namespace BuildingBlocks.Infrastructure.DistributedLock;

/// <summary>
/// PostgreSQL-based distributed lock using advisory locks
/// Provides fast, reliable distributed locking without external dependencies
/// Perfect for microservices that already use PostgreSQL
/// </summary>
public class PostgresDistributedLock(
    string connectionString,
    ILogger<PostgresDistributedLock> logger)
    : IDistributedLock
{
    private readonly string _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    private readonly ILogger<PostgresDistributedLock> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private static readonly TimeSpan DefaultExpirationTime = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DefaultWaitTime = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan DefaultRetryTime = TimeSpan.FromMilliseconds(100);

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
        var lockId = GetLockId(key);

        _logger.LogDebug("Attempting to acquire lock for key {LockKey} (ID: {LockId})", key, lockId);

        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < wait)
        {
            var lockHandle = await TryAcquireAsync(key, expiration, cancellationToken);
            if (lockHandle != null)
            {
                _logger.LogInformation(
                    "Successfully acquired lock for key {LockKey} (ID: {LockId}) after {ElapsedMs}ms",
                    key,
                    lockId,
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
            "Failed to acquire lock for key {LockKey} (ID: {LockId}) after {WaitMs}ms",
            key,
            lockId,
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

        var lockId = GetLockId(key);

        try
        {
            // Use PostgreSQL advisory locks (session-based)
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Try to acquire advisory lock (non-blocking)
            await using var cmd = new NpgsqlCommand("SELECT pg_try_advisory_lock(@lockId)", connection);
            cmd.Parameters.AddWithValue("lockId", lockId);

            var acquired = (bool?)await cmd.ExecuteScalarAsync(cancellationToken);

            if (acquired == true)
            {
                _logger.LogDebug("Acquired advisory lock for key {LockKey} (ID: {LockId})", key, lockId);
                return new PostgresLockHandle(connection, lockId, key, _logger);
            }

            // Failed to acquire lock
            await connection.CloseAsync();
            await connection.DisposeAsync();
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring lock for key {LockKey} (ID: {LockId})", key, lockId);
            return null;
        }
    }

    public async Task<bool> IsLockedAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Lock key cannot be null or empty", nameof(key));

        var lockId = GetLockId(key);

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Check if advisory lock exists
            await using var cmd = new NpgsqlCommand(
                @"SELECT COUNT(*) > 0 
                  FROM pg_locks 
                  WHERE locktype = 'advisory' 
                    AND classid = 0 
                    AND objid = @lockId",
                connection);
            cmd.Parameters.AddWithValue("lockId", lockId);

            return (bool)await cmd.ExecuteScalarAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking lock status for key {LockKey} (ID: {LockId})", key, lockId);
            return false;
        }
    }

    public Task<bool> ExtendLockAsync(
        string key,
        TimeSpan expirationTime,
        CancellationToken cancellationToken = default)
    {
        // PostgreSQL advisory locks don't expire automatically
        // They're held until explicitly released or connection closes
        // So extension is not needed
        _logger.LogDebug("Lock extension not needed for PostgreSQL advisory locks (key: {LockKey})", key);
        return Task.FromResult(true);
    }

    /// <summary>
    /// Convert string key to int64 hash for PostgreSQL advisory lock
    /// </summary>
    private static long GetLockId(string key)
    {
        unchecked
        {
            return key.Aggregate<char, long>(5381, (current, c) => ((current << 5) + current) + c);
        }
    }

    /// <summary>
    /// Lock handle that releases the lock when disposed
    /// </summary>
    private class PostgresLockHandle(
        NpgsqlConnection connection,
        long lockId,
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
                // Release advisory lock
                using var cmd = new NpgsqlCommand("SELECT pg_advisory_unlock(@lockId)", connection);
                cmd.Parameters.AddWithValue("lockId", lockId);
                cmd.ExecuteScalar();

                logger.LogDebug("Released advisory lock for key {LockKey} (ID: {LockId})", key, lockId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error releasing lock for key {LockKey} (ID: {LockId})", key, lockId);
            }
            finally
            {
                connection?.Close();
                connection?.Dispose();
                _disposed = true;
            }
        }
    }
}
