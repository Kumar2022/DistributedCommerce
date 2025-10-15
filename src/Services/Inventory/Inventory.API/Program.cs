using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Authentication.Extensions;
using BuildingBlocks.EventBus.Kafka;
using BuildingBlocks.EventBus.Dispatcher;
using FluentValidation;
using Inventory.Application;
using Inventory.Application.Commands;
using Inventory.Application.EventHandlers;
using Inventory.Application.Validators;
using Inventory.Infrastructure.BackgroundServices;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Repositories;
using Inventory.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add Controllers
builder.Services.AddControllers();

// Add Authentication and Authorization
builder.Services.AddDistributedAuthentication(builder.Configuration);
builder.Services.AddCurrentUserService();

// Add Application Layer
builder.Services.AddInventoryApplication();

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("InventoryDb") 
    ?? "Host=localhost;Database=inventory_db;Username=postgres;Password=postgres";

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add Repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Add Query Services
builder.Services.AddScoped<Inventory.Application.Queries.IInventoryQueryService, InventoryQueryService>();

// Add Kafka EventBus
var kafkaBootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
builder.Services.AddKafkaEventBus(kafkaBootstrapServers, "inventory-service");

// Add Event Dispatcher (single consumer per topic pattern)
builder.Services.AddEventDispatcher();

// Register event handlers in DI
builder.Services.AddScoped<InventoryReservationRequestedEventHandler>();
builder.Services.AddScoped<InventoryReservationReleasedEventHandler>();

// Register dispatching consumer (one for order events topic)
builder.Services.AddDispatchingKafkaConsumer(
    kafkaBootstrapServers,
    "inventory-service-order-events",
    "domain.order.events");

// Add Background Services
builder.Services.AddHostedService<ReservationExpirationService>();

// Add Domain Event Handlers (from Infrastructure)
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(InventoryDbContext).Assembly);
});

// Add API services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Inventory API", 
        Version = "v1",
        Description = "Inventory Service with Stock Management and Reservations"
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

    // Include XML comments if available
    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

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

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "inventory-db");

var app = builder.Build();

// Register event handlers with dispatcher
using (var scope = app.Services.CreateScope())
{
    var dispatcher = scope.ServiceProvider.GetRequiredService<IEventDispatcher>();
    
    var reservationRequestedHandler = scope.ServiceProvider.GetRequiredService<InventoryReservationRequestedEventHandler>();
    var reservationReleasedHandler = scope.ServiceProvider.GetRequiredService<InventoryReservationReleasedEventHandler>();
    
    dispatcher.RegisterHandler("InventoryReservationRequestedEvent", reservationRequestedHandler);
    dispatcher.RegisterHandler("InventoryReservationReleasedEvent", reservationReleasedHandler);
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map Controllers
app.MapControllers();

// Map Health Checks
app.MapHealthChecks("/health");

// Welcome endpoint
app.MapGet("/", () => new
{
    service = "Inventory API",
    version = "v1.0.0",
    status = "Running",
    features = new[] { "Stock Management", "Reservations", "Optimistic Concurrency", "Controllers" },
    timestamp = DateTime.UtcNow
})
.ExcludeFromDescription();

app.Run();

// Make Program accessible to integration tests
public partial class Program { }
