using Payment.Application;
using Payment.Application.EventHandlers;
using Payment.Infrastructure;
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
        Title = "Payment API",
        Version = "v1",
        Description = "Payment Service with Stripe Integration and Outbox Pattern"
    });

    // Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
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

// Add Authentication
builder.Services.AddDistributedAuthentication(builder.Configuration);
builder.Services.AddCurrentUserService();

// Application and Infrastructure
builder.Services.AddPaymentApplication();
builder.Services.AddPaymentInfrastructure(builder.Configuration);

// Add Kafka EventBus
var kafkaBootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
builder.Services.AddKafkaEventBus(kafkaBootstrapServers, "payment-service");

// Add Event Dispatcher (single consumer per topic pattern)
builder.Services.AddEventDispatcher();

// Register event handlers in DI
builder.Services.AddScoped<PaymentRequestedEventHandler>();
builder.Services.AddScoped<PaymentRefundRequestedEventHandler>();

// Register dispatching consumer (one for order events topic)
builder.Services.AddDispatchingKafkaConsumer(
    kafkaBootstrapServers,
    "payment-service-order-events",
    "domain.order.events");

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

// Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Register event handlers with dispatcher
using (var scope = app.Services.CreateScope())
{
    var dispatcher = scope.ServiceProvider.GetRequiredService<IEventDispatcher>();
    
    var paymentRequestedHandler = scope.ServiceProvider.GetRequiredService<PaymentRequestedEventHandler>();
    var refundRequestedHandler = scope.ServiceProvider.GetRequiredService<PaymentRefundRequestedEventHandler>();
    
    dispatcher.RegisterHandler("PaymentRequestedEvent", paymentRequestedHandler);
    dispatcher.RegisterHandler("PaymentRefundRequestedEvent", refundRequestedHandler);
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors();

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
    service = "Payment API",
    version = "v1.0.0",
    status = "Running",
    features = new[] { "Stripe Integration", "Outbox Pattern", "Idempotency", "Controllers" },
    timestamp = DateTime.UtcNow
})
.ExcludeFromDescription();

app.Run();

// Make Program class accessible to integration tests
public partial class Program { }
