using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ApiGateway.Extensions;

/// <summary>
/// Extension methods for configuring authentication and authorization
/// </summary>
public static class AuthenticationExtensions
{
    public static IServiceCollection AddGatewayAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Authentication:Jwt");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = jwtSettings["Authority"];
                options.Audience = jwtSettings["Audience"];
                options.RequireHttpsMetadata = jwtSettings.GetValue<bool>("RequireHttpsMetadata");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = jwtSettings.GetValue<bool>("ValidateIssuer"),
                    ValidateAudience = jwtSettings.GetValue<bool>("ValidateAudience"),
                    ValidateLifetime = jwtSettings.GetValue<bool>("ValidateLifetime"),
                    ValidateIssuerSigningKey = jwtSettings.GetValue<bool>("ValidateIssuerSigningKey"),
                    ClockSkew = TimeSpan.FromMinutes(5)
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<Program>>();
                        logger.LogWarning("Authentication failed: {Error}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<Program>>();
                        logger.LogDebug("Token validated for user: {User}", 
                            context.Principal?.Identity?.Name);
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<Program>>();
                        logger.LogWarning("Authentication challenge: {Error}", context.Error);
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }

    public static IServiceCollection AddGatewayAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy("authenticated", policy =>
            {
                policy.RequireAuthenticatedUser();
            })
            .AddPolicy("admin", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Admin");
            })
            .AddPolicy("user", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("User", "Admin");
            })
            .AddPolicy("customer-service", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("CustomerService", "Admin");
            });

        return services;
    }
}
