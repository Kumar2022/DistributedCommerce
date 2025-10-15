using BuildingBlocks.Authentication.Configuration;
using BuildingBlocks.Authentication.Handlers;
using BuildingBlocks.Authentication.Jwt;
using BuildingBlocks.Authentication.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AuthPolicies = BuildingBlocks.Authentication.Authorization.PolicyNames;
using AuthRoles = BuildingBlocks.Authentication.Authorization.Roles;
using AuthClaims = BuildingBlocks.Authentication.Authorization.ClaimTypes;
using AuthPermissions = BuildingBlocks.Authentication.Authorization.Permissions;

namespace BuildingBlocks.Authentication.Extensions;

/// <summary>
/// Extension methods for configuring JWT authentication
/// </summary>
public static class AuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// Add JWT authentication to the service collection
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind JWT options from configuration
        var jwtOptions = new JwtOptions();
        configuration.GetSection(JwtOptions.SectionName).Bind(jwtOptions);

        // Validate configuration
        ValidateJwtOptions(jwtOptions);

        // Register JWT options as singleton
        services.AddSingleton(jwtOptions);

        // Register HTTP context accessor (required for CurrentUserService)
        services.AddHttpContextAccessor();

        // Register JWT token service
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Register current user service
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Configure JWT bearer authentication
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = jwtOptions.ValidateIssuer,
                    ValidateAudience = jwtOptions.ValidateAudience,
                    ValidateLifetime = jwtOptions.ValidateLifetime,
                    ValidateIssuerSigningKey = jwtOptions.ValidateIssuerSigningKey,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                    ClockSkew = TimeSpan.FromMinutes(jwtOptions.ClockSkewMinutes),
                    RequireExpirationTime = jwtOptions.RequireExpirationTime,
                    RequireSignedTokens = jwtOptions.RequireSignedTokens
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.TryAdd("Token-Expired", "true");
                        }

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context => Task.CompletedTask,
                    OnMessageReceived = context =>
                    {
                        // Support token from query string for SignalR/WebSocket connections
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        // Add enhanced authorization policies
        services.AddAuthorizationBuilder()
            // Add enhanced authorization policies
            .AddPolicy(AuthPolicies.Authenticated, policy =>
                policy.RequireAuthenticatedUser())
            // Add enhanced authorization policies
            .AddPolicy(AuthPolicies.AdminOnly, policy =>
                policy.RequireRole(AuthRoles.Admin))
            // Add enhanced authorization policies
            .AddPolicy(AuthPolicies.CustomerOnly, policy =>
                policy.RequireRole(AuthRoles.Customer))
            // Add enhanced authorization policies
            .AddPolicy(AuthPolicies.CustomerOrAdmin, policy =>
                policy.RequireRole(AuthRoles.Customer, AuthRoles.Admin))
            // Add enhanced authorization policies
            .AddPolicy(AuthPolicies.ServiceAccount, policy =>
                policy.RequireClaim(AuthClaims.ServiceAccount, "true"))
            // Add enhanced authorization policies
            .AddPolicy(AuthPolicies.ManageOrders, policy =>
                policy.RequireAssertion(context =>
                    context.User.IsInRole(AuthRoles.Admin) ||
                    context.User.HasClaim(AuthClaims.Permission, AuthPermissions.OrdersWrite)))
            // Add enhanced authorization policies
            .AddPolicy(AuthPolicies.ManageProducts, policy =>
                policy.RequireAssertion(context =>
                    context.User.IsInRole(AuthRoles.Admin) ||
                    context.User.HasClaim(AuthClaims.Permission, AuthPermissions.ProductsWrite)))
            // Add enhanced authorization policies
            .AddPolicy(AuthPolicies.ManageInventory, policy =>
                policy.RequireAssertion(context =>
                    context.User.IsInRole(AuthRoles.Admin) ||
                    context.User.IsInRole(AuthRoles.WarehouseManager) ||
                    context.User.HasClaim(AuthClaims.Permission, AuthPermissions.InventoryWrite)))
            // Add enhanced authorization policies
            .AddPolicy(AuthPolicies.ManagePayments, policy =>
                policy.RequireAssertion(context =>
                    context.User.IsInRole(AuthRoles.Admin) ||
                    context.User.HasClaim(AuthClaims.Permission, AuthPermissions.PaymentsProcess)))
            // Add enhanced authorization policies
            .AddPolicy(AuthPolicies.ManageShipments, policy =>
                policy.RequireAssertion(context =>
                    context.User.IsInRole(AuthRoles.Admin) ||
                    context.User.HasClaim(AuthClaims.Permission, AuthPermissions.ShipmentsCreate)))
            // Add enhanced authorization policies
            .AddPolicy(AuthPolicies.ViewAnalytics, policy =>
                policy.RequireAssertion(context =>
                    context.User.IsInRole(AuthRoles.Admin) ||
                    context.User.IsInRole(AuthRoles.Analyst) ||
                    context.User.HasClaim(AuthClaims.Permission, AuthPermissions.AnalyticsView)))
            // Add enhanced authorization policies
            .AddPolicy(AuthPolicies.ManageUsers, policy =>
                policy.RequireAssertion(context =>
                    context.User.IsInRole(AuthRoles.Admin) ||
                    context.User.HasClaim(AuthClaims.Permission, AuthPermissions.UsersWrite)));

        return services;
    }

    /// <summary>
    /// Add distributed authentication (alias for AddJwtAuthentication for backward compatibility)
    /// </summary>
    public static IServiceCollection AddDistributedAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddJwtAuthentication(configuration);
    }

    /// <summary>
    /// Add current user service
    /// </summary>
    public static IServiceCollection AddCurrentUserService(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        return services;
    }

    /// <summary>
    /// Add API key authentication for service-to-service calls
    /// </summary>
    public static IServiceCollection AddApiKeyAuthentication(
        this IServiceCollection services,
        Action<ApiKeyAuthenticationOptions> configureOptions)
    {
        services.AddAuthentication()
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationOptions.DefaultScheme,
                configureOptions);

        return services;
    }

    /// <summary>
    /// Validate JWT options
    /// </summary>
    private static void ValidateJwtOptions(JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Secret))
            throw new InvalidOperationException(
                "JWT Secret is required. Configure it in appsettings.json under Jwt:Secret");

        if (options.Secret.Length < 32)
            throw new InvalidOperationException("JWT Secret must be at least 32 characters long for HS256");

        if (string.IsNullOrWhiteSpace(options.Issuer))
            throw new InvalidOperationException("JWT Issuer is required");

        if (string.IsNullOrWhiteSpace(options.Audience))
            throw new InvalidOperationException("JWT Audience is required");

        if (options.AccessTokenExpirationMinutes <= 0)
            throw new InvalidOperationException("Access token expiration must be greater than 0");
    }
}
