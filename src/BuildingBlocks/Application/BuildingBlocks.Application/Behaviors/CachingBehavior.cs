using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Text.Json;

namespace BuildingBlocks.Application.Behaviors;

/// <summary>
/// Pipeline behavior that caches query results
/// Only applies to queries (IQuery<TResponse>)
/// </summary>
/// <typeparam name="TRequest">The request type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public sealed class CachingBehavior<TRequest, TResponse>(IDistributedCache? cache = null)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Skip caching if the cache is not configured or the request is not cacheable
        if (cache is null || request is not ICacheableQuery cacheableQuery)
        {
            return await next();
        }

        var cacheKey = GenerateCacheKey(request, cacheableQuery);

        // Try to get from cache
        var cachedResult = await cache.GetStringAsync(cacheKey, cancellationToken);

        if (cachedResult is not null)
        {
            return JsonSerializer.Deserialize<TResponse>(cachedResult)!;
        }

        // Execute handler
        var response = await next();

        // Cache the result
        var serializedResponse = JsonSerializer.Serialize(response);
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = cacheableQuery.CacheDuration
        };

        await cache.SetStringAsync(cacheKey, serializedResponse, cacheOptions, cancellationToken);

        return response;
    }

    private static string GenerateCacheKey(TRequest request, ICacheableQuery cacheableQuery)
    {
        var requestType = request.GetType().Name;
        var requestJson = JsonSerializer.Serialize(request);
        var requestHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(requestJson)));

        return $"{cacheableQuery.CacheKeyPrefix}:{requestType}:{requestHash}";
    }
}

/// <summary>
/// Marker interface for queries that should be cached
/// </summary>
public interface ICacheableQuery
{
    /// <summary>
    /// The cache key prefix (e.g., "orders", "products")
    /// </summary>
    string CacheKeyPrefix { get; }

    /// <summary>
    /// How long to cache the result
    /// </summary>
    TimeSpan CacheDuration { get; }
}
