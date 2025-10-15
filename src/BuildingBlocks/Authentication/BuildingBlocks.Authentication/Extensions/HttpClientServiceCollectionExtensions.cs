using BuildingBlocks.Authentication.Jwt;
using BuildingBlocks.Authentication.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Authentication.Extensions;

/// <summary>
/// Extension methods for configuring HTTP clients with authentication
/// </summary>
public static class HttpClientServiceCollectionExtensions
{
    /// <summary>
    /// Add HTTP client with JWT token propagation
    /// </summary>
    public static IHttpClientBuilder AddAuthenticatedHttpClient(
        this IServiceCollection services,
        string name)
    {
        return services.AddHttpClient(name)
            .AddHttpMessageHandler<JwtTokenDelegatingHandler>();
    }
    
    /// <summary>
    /// Add HTTP client with JWT token propagation and service account authentication
    /// </summary>
    public static IHttpClientBuilder AddServiceHttpClient(
        this IServiceCollection services,
        string name,
        Action<IServiceProvider, HttpClient> configureClient)
    {
        services.AddHttpContextAccessor();
        services.AddTransient<JwtTokenDelegatingHandler>();
        
        return services.AddHttpClient(name, configureClient)
            .AddHttpMessageHandler<JwtTokenDelegatingHandler>();
    }
}
