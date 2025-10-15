using Catalog.Application;
using Catalog.Infrastructure;
using BuildingBlocks.Authentication.Extensions;
using BuildingBlocks.EventBus.Kafka;
using Microsoft.OpenApi.Models;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting Catalog Service API");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog((context, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration));

    // Add services to the container
    builder.Services.AddControllers();
    
    // Add Authentication and Authorization
    builder.Services.AddDistributedAuthentication(builder.Configuration);
    builder.Services.AddCurrentUserService();
    
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { 
            Title = "Catalog Service API", 
            Version = "v1",
            Description = "FAANG-Scale Distributed Commerce - Catalog Service"
        });
        
        // Add JWT Authentication to Swagger
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
    });

    // Add Application Layer
    builder.Services.AddApplicationServices();

    // Add Infrastructure Layer
    builder.Services.AddCatalogInfrastructure(builder.Configuration);

    // Add Kafka Event Bus
    var kafkaBootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
    builder.Services.AddKafkaEventBus(kafkaBootstrapServers, "catalog-service");

    // Add Health Checks
    builder.Services.AddHealthChecks()
        .AddNpgSql(
            builder.Configuration.GetConnectionString("CatalogDb")!,
            name: "catalogdb",
            tags: ["db", "postgres"]);

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

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog Service API v1"));
    }

    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();

    app.UseCors();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready");
    app.MapHealthChecks("/health/live");

    Log.Information("Catalog Service API started successfully");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Catalog Service API terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
