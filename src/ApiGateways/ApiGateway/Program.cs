using ApiGateway.Extensions;
using ApiGateway.Middleware;
using Serilog;
using Yarp.ReverseProxy.Transforms;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "ApiGateway")
    .CreateLogger();

try
{
    Log.Information("Starting Distributed Commerce API Gateway");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog for logging
    builder.Host.UseSerilog();

    // Add core services
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    
    // Add Swagger/OpenAPI
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Distributed Commerce API Gateway",
            Version = "v1.0.0",
            Description = "Enterprise-grade API Gateway for Distributed Commerce Platform using YARP Reverse Proxy",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "Platform Team",
                Email = "platform@distributedcommerce.com"
            }
        });

        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT"
        });

        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // Configure CORS
    var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? ["http://localhost:3000", "http://localhost:4200"];

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowSpecificOrigins", corsBuilder =>
        {
            corsBuilder
                .WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .WithExposedHeaders("X-Correlation-ID", "X-Response-Time-Ms", "X-RateLimit-Limit", "X-RateLimit-Remaining");
        });
    });

    // Add Authentication & Authorization (using extension methods)
    builder.Services.AddGatewayAuthentication(builder.Configuration);
    builder.Services.AddGatewayAuthorization();

    // Add Rate Limiting (using extension methods)
    builder.Services.AddGatewayRateLimiting(builder.Configuration);

    // Add Observability (using extension methods)
    builder.Services.AddGatewayObservability(builder.Configuration, builder.Environment);

    // Configure YARP Reverse Proxy with transforms
    builder.Services.AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
        .AddTransforms(transformBuilderContext =>
        {
            // Add correlation ID to all proxied requests
            transformBuilderContext.AddRequestTransform(transformContext =>
            {
                var correlationId = transformContext.HttpContext.Items["X-Correlation-ID"]?.ToString()
                    ?? transformContext.HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                    ?? Guid.NewGuid().ToString();

                transformContext.ProxyRequest.Headers.Add("X-Correlation-ID", correlationId);
                return ValueTask.CompletedTask;
            });

            // Forward client IP
            transformBuilderContext.AddRequestTransform(transformContext =>
            {
                var clientIp = transformContext.HttpContext.Connection.RemoteIpAddress?.ToString();
                if (!string.IsNullOrEmpty(clientIp))
                {
                    transformContext.ProxyRequest.Headers.Add("X-Real-IP", clientIp);
                    transformContext.ProxyRequest.Headers.Add("X-Forwarded-For", clientIp);
                }
                return ValueTask.CompletedTask;
            });

            // Add timestamp
            transformBuilderContext.AddRequestTransform(transformContext =>
            {
                transformContext.ProxyRequest.Headers.Add("X-Gateway-Timestamp", 
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
                return ValueTask.CompletedTask;
            });
        });

    // Add Health Checks
    builder.Services.AddHealthChecks();

    // Build the application
    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "Distributed Commerce API Gateway";
            options.DisplayRequestDuration();
        });
    }
    else
    {
        // Production error handling
        app.UseGlobalExceptionHandler();
    }

    // Middleware pipeline (order matters!)
    app.UseHttpsRedirection();
    app.UseCors("AllowSpecificOrigins");
    
    // Custom middleware
    app.UseCorrelationId();
    app.UsePerformanceMonitoring();
    
    app.UseRouting();
    
    // Rate limiting (before authentication)
    app.UseRateLimiter();
    
    // Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // Map health check endpoint
    app.MapHealthChecks("/health").AllowAnonymous();
    app.MapHealthChecks("/health/ready").AllowAnonymous();
    app.MapHealthChecks("/health/live").AllowAnonymous();

    // Map gateway info endpoint
    app.MapGet("/api/gateway/info", () => new
    {
        service = "API Gateway",
        version = "1.0.0",
        environment = app.Environment.EnvironmentName,
        timestamp = DateTime.UtcNow,
        status = "operational"
    })
    .WithName("GetGatewayInfo")
    .WithTags("Gateway")
    .AllowAnonymous();

    // Map gateway status endpoint
    app.MapGet("/api/gateway/status", () => Results.Ok(new
    {
        status = "healthy",
        uptime = DateTime.UtcNow,
        services = new[]
        {
            new { name = "Identity", cluster = "identity-cluster", status = "healthy" },
            new { name = "Order", cluster = "order-cluster", status = "healthy" },
            new { name = "Payment", cluster = "payment-cluster", status = "healthy" },
            new { name = "Inventory", cluster = "inventory-cluster", status = "healthy" },
            new { name = "Catalog", cluster = "catalog-cluster", status = "healthy" },
            new { name = "Shipping", cluster = "shipping-cluster", status = "healthy" },
            new { name = "Notification", cluster = "notification-cluster", status = "healthy" },
            new { name = "Analytics", cluster = "analytics-cluster", status = "healthy" }
        }
    }))
    .WithName("GetGatewayStatus")
    .WithTags("Gateway")
    .RequireAuthorization("admin");

    // Map YARP reverse proxy
    app.MapReverseProxy(proxyPipeline =>
    {
        // Add custom middleware to the proxy pipeline if needed
        proxyPipeline.Use(async (context, next) =>
        {
            Log.Debug("Proxying request to: {Destination}", context.Request.Path);
            await next();
            Log.Debug("Proxy response status: {StatusCode}", context.Response.StatusCode);
        });
    });

    Log.Information("API Gateway started successfully on {Urls}", 
        string.Join(", ", app.Urls));

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API Gateway terminated unexpectedly");
    throw;
}
finally
{
    Log.Information("API Gateway shutting down");
    await Log.CloseAndFlushAsync();
}

