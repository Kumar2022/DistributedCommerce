using Identity.Application;
using Identity.Infrastructure;
using BuildingBlocks.Authentication.Extensions;
using BuildingBlocks.Authentication.Middleware;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add controllers
builder.Services.AddControllers();

// Add API Explorer for Swagger
builder.Services.AddEndpointsApiExplorer();

// Add Swagger with enhanced configuration
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Identity API",
        Version = "v1.0.0",
        Description = "Authentication and Authorization Service - Enterprise-grade identity management with JWT tokens",
        Contact = new OpenApiContact
        {
            Name = "Distributed Commerce Team",
            Email = "support@distributedcommerce.com"
        }
    });

    // Add JWT authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments if available
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Add JWT Authentication using BuildingBlocks
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add Application and Infrastructure layers
builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add health checks
builder.Services.AddHealthChecks();
// TODO: Add database health check when AspNetCore.HealthChecks.Npgsql package is added
// .AddNpgSql(connectionString, name: "database", tags: new[] { "db", "sql", "postgresql" });

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "Identity API Documentation";
    });
}

app.UseHttpsRedirection();
app.UseCors();

// Add Correlation ID middleware
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");

// Welcome endpoint
app.MapGet("/", () => new
{
    service = "Identity API",
    version = "v1.0.0",
    status = "Running",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName
})
.WithName("Welcome")
.WithTags("System")
.ExcludeFromDescription();

app.Run();

