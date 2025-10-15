using Order.Application;
using Order.Application.EventHandlers;
using Order.Infrastructure;
using BuildingBlocks.Authentication.Extensions;
using BuildingBlocks.EventBus.Kafka;
using BuildingBlocks.EventBus.Dispatcher;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add Controllers
builder.Services.AddControllers();

// Add API Explorer and Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Order API",
        Version = "v1",
        Description = "Order Service with Event Sourcing and CQRS"
    });

    // Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
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
    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Add Authentication and Authorization
builder.Services.AddDistributedAuthentication(builder.Configuration);
builder.Services.AddCurrentUserService();

// Add Application and Infrastructure layers
builder.Services.AddOrderApplication();
builder.Services.AddOrderInfrastructure(builder.Configuration);

// Add Kafka EventBus
var kafkaBootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
builder.Services.AddKafkaEventBus(kafkaBootstrapServers, "order-service");

// Add Event Dispatcher (single consumer per topic pattern)
builder.Services.AddEventDispatcher();

// Register event handlers in DI
builder.Services.AddScoped<InventoryReservationConfirmedEventHandler>();
builder.Services.AddScoped<InventoryReservationFailedEventHandler>();
builder.Services.AddScoped<PaymentConfirmedEventHandler>();
builder.Services.AddScoped<PaymentFailedEventHandler>();

// Register dispatching consumers (one per topic)
builder.Services.AddDispatchingKafkaConsumer(
    kafkaBootstrapServers,
    "order-service-inventory-events",
    "domain.inventory.events");

builder.Services.AddDispatchingKafkaConsumer(
    kafkaBootstrapServers,
    "order-service-payment-events",
    "domain.payment.events");

// Register handlers with dispatcher (after building the app)
// This will be done in a configure method below

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

var app = builder.Build();

// Register event handlers with dispatcher
using (var scope = app.Services.CreateScope())
{
    var dispatcher = scope.ServiceProvider.GetRequiredService<IEventDispatcher>();
    
    // Inventory events
    var inventoryConfirmedHandler = scope.ServiceProvider.GetRequiredService<InventoryReservationConfirmedEventHandler>();
    var inventoryFailedHandler = scope.ServiceProvider.GetRequiredService<InventoryReservationFailedEventHandler>();
    
    dispatcher.RegisterHandler("InventoryReservationConfirmedEvent", inventoryConfirmedHandler);
    dispatcher.RegisterHandler("InventoryReservationFailedEvent", inventoryFailedHandler);
    
    // Payment events
    var paymentConfirmedHandler = scope.ServiceProvider.GetRequiredService<PaymentConfirmedEventHandler>();
    var paymentFailedHandler = scope.ServiceProvider.GetRequiredService<PaymentFailedEventHandler>();
    
    dispatcher.RegisterHandler("PaymentConfirmedEvent", paymentConfirmedHandler);
    dispatcher.RegisterHandler("PaymentFailedEvent", paymentFailedHandler);
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors();

// Add Authentication and Authorization middleware
app.UseAuthentication();
app.UseAuthorization();
app.UseCorrelationId();

// Map Controllers
app.MapControllers();

// Health check endpoint
app.MapHealthChecks("/health");

// Welcome endpoint
app.MapGet("/", () => new
{
    service = "Order API",
    version = "v1.0.0",
    status = "Running",
    features = new[] { "Event Sourcing", "CQRS", "Marten", "Domain-Driven Design", "Controllers" },
    timestamp = DateTime.UtcNow
})
.ExcludeFromDescription();

app.Run();

// Make the implicit Program class public for integration testing
public partial class Program { }

