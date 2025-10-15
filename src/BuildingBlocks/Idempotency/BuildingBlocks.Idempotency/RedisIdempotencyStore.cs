using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Idempotency;

/// <summary>
/// Redis-based implementation of idempotency store
/// Provides distributed idempotency tracking across multiple service instances
/// </summary>
public class RedisIdempotencyStore(
    IDistributedCache cache,
    ILogger<RedisIdempotencyStore> logger)
    : IIdempotencyStore
{
    private readonly IDistributedCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly ILogger<RedisIdempotencyStore> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);
    private const string KeyPrefix = "idempotency:";

    public async Task<bool> IsProcessedAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency key cannot be null or empty", nameof(idempotencyKey));

        var key = GetCacheKey(idempotencyKey);
        
        try
        {
            var value = await _cache.GetStringAsync(key, cancellationToken);
            var isProcessed = !string.IsNullOrEmpty(value);

            _logger.LogDebug(
                "Idempotency check for key {IdempotencyKey}: {IsProcessed}", 
                idempotencyKey, 
                isProcessed ? "Already Processed" : "Not Processed");

            return isProcessed;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking idempotency for key {IdempotencyKey}",
                idempotencyKey);
            
            // On error, assume not processed to avoid blocking operations
            // This is safer than risking duplicate processing
            return false;
        }
    }

    public async Task MarkAsProcessedAsync(
        string idempotencyKey, 
        object? result = null, 
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency key cannot be null or empty", nameof(idempotencyKey));

        var key = GetCacheKey(idempotencyKey);
        var expirationTime = ttl ?? DefaultTtl;

        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expirationTime
            };

            var value = result != null 
                ? JsonSerializer.Serialize(new IdempotencyRecord
                {
                    ProcessedAt = DateTime.UtcNow,
                    Result = result
                })
                : JsonSerializer.Serialize(new IdempotencyRecord
                {
                    ProcessedAt = DateTime.UtcNow
                });

            await _cache.SetStringAsync(key, value, options, cancellationToken);

            _logger.LogInformation(
                "Marked idempotency key {IdempotencyKey} as processed with TTL {Ttl}",
                idempotencyKey,
                expirationTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error marking idempotency key {IdempotencyKey} as processed",
                idempotencyKey);
            
            // Re-throw to ensure caller knows the operation failed
            throw;
        }
    }

    public async Task<TResult?> GetResultAsync<TResult>(
        string idempotencyKey, 
        CancellationToken cancellationToken = default) where TResult : class
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency key cannot be null or empty", nameof(idempotencyKey));

        var key = GetCacheKey(idempotencyKey);

        try
        {
            var value = await _cache.GetStringAsync(key, cancellationToken);
            
            if (string.IsNullOrEmpty(value))
            {
                _logger.LogDebug(
                    "No result found for idempotency key {IdempotencyKey}",
                    idempotencyKey);
                return null;
            }

            var record = JsonSerializer.Deserialize<IdempotencyRecord>(value);
            
            if (record?.Result == null)
            {
                return null;
            }

            // Deserialize the result to the requested type
            var resultJson = JsonSerializer.Serialize(record.Result);
            var result = JsonSerializer.Deserialize<TResult>(resultJson);

            _logger.LogDebug(
                "Retrieved result for idempotency key {IdempotencyKey}",
                idempotencyKey);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving result for idempotency key {IdempotencyKey}",
                idempotencyKey);
            
            return null;
        }
    }

    private static string GetCacheKey(string idempotencyKey) => $"{KeyPrefix}{idempotencyKey}";

    private class IdempotencyRecord
    {
        public DateTime ProcessedAt { get; set; }
        public object? Result { get; set; }
    }
}
