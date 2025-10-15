# Low-Level Design (LLD)

## Technology Specifications

### Runtime
- .NET 9.0
- C# 13 with nullable reference types enabled
- Implicit usings enabled

### API Gateway
- YARP 2.1.0 (Yet Another Reverse Proxy)
- Microsoft.AspNetCore.Authentication.JwtBearer 9.0.0
- Built-in .NET 9 rate limiting
- Polly 8.5.0 for resilience

### Backend Services
- ASP.NET Core 9.0 (minimal APIs)
- Entity Framework Core 9.0.9
- Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4
- MediatR 12.4.1
- FluentValidation
- AutoMapper

### Messaging
- Confluent.Kafka 2.3.0
- Apache Kafka 7.5.0 (Confluent Platform)
- Avro schema with Schema Registry

### Caching
- Microsoft.Extensions.Caching.StackExchangeRedis 9.0.9
- StackExchange.Redis 2.9.25
- Redis 7-alpine

### Observability
- OpenTelemetry.Extensions.Hosting 1.9.0+
- OpenTelemetry.Instrumentation.AspNetCore 1.9.0+
- OpenTelemetry.Instrumentation.Http 1.9.0+
- OpenTelemetry.Exporter.OpenTelemetryProtocol 1.9.0+
- Serilog.AspNetCore 8.0.3

### Testing
- xUnit 2.9.2
- FluentAssertions 7.0.0
- Moq 4.20.70
- AutoFixture 4.18.1
- Bogus 35.6.1 (test data generation)
- Testcontainers

## Service Implementation

### Clean Architecture Layers

Each service follows 4-layer architecture:

```
Service.API/                    # Presentation Layer
├── Controllers/                # REST endpoints
├── Middleware/                 # Request pipeline
├── Extensions/                 # Service registration
└── Program.cs                  # Entry point

Service.Application/            # Application Layer
├── Commands/                   # Write operations
│   ├── CreateXCommand.cs
│   └── CreateXCommandHandler.cs
├── Queries/                    # Read operations
│   ├── GetXQuery.cs
│   └── GetXQueryHandler.cs
├── DTOs/                       # Data transfer objects
├── Mappings/                   # AutoMapper profiles
├── Validators/                 # FluentValidation rules
└── Services/                   # Application services

Service.Domain/                 # Domain Layer
├── Aggregates/                 # Aggregate roots
├── Entities/                   # Domain entities
├── ValueObjects/               # Value objects
├── Events/                     # Domain events
├── Exceptions/                 # Domain exceptions
└── Interfaces/                 # Repository interfaces

Service.Infrastructure/         # Infrastructure Layer
├── Persistence/
│   ├── DbContext.cs           # EF Core context
│   ├── Configurations/        # Entity configurations
│   ├── Migrations/            # DB migrations
│   └── Repositories/          # Repository implementations
├── EventBus/                  # Kafka producers/consumers
├── Caching/                   # Redis implementation
└── ExternalServices/          # Third-party integrations
```

## Database Design

### Entity Framework Core Configuration

**DbContext Setup**
```csharp
public class CatalogDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
```

**Entity Configuration Example**
```csharp
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.Property(p => p.Price)
            .HasColumnType("decimal(18,2)");
            
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId);
            
        builder.HasIndex(p => p.Name);
    }
}
```

### Database Schema per Service

**Identity Service**
- users (id, email, password_hash, role, created_at, updated_at)
- refresh_tokens (id, user_id, token, expires_at)
- user_roles (id, user_id, role_name)

**Catalog Service**
- products (id, name, description, price, category_id, created_at)
- categories (id, name, parent_id)
- product_images (id, product_id, url, is_primary)

**Order Service**
- orders (id, user_id, status, total_amount, created_at)
- order_items (id, order_id, product_id, quantity, price)
- saga_state (id, order_id, current_step, state, created_at)

**Payment Service**
- payments (id, order_id, amount, status, gateway, created_at)
- payment_methods (id, user_id, type, details)
- idempotency_keys (key, request_hash, response, created_at)

**Inventory Service**
- inventory (id, product_id, quantity, warehouse_id)
- reservations (id, product_id, order_id, quantity, expires_at)
- warehouses (id, name, location)

**Shipping Service**
- shipments (id, order_id, tracking_number, carrier, status)
- tracking_events (id, shipment_id, status, location, timestamp)

**Notification Service**
- notifications (id, user_id, type, template_id, status, created_at)
- templates (id, name, subject, body, type)

**Analytics Service**
- events (id, event_type, user_id, data, timestamp)
- aggregated_metrics (id, metric_name, value, period, timestamp)

## CQRS Implementation

### Command Pattern
```csharp
// Command
public record CreateProductCommand(
    string Name, 
    string Description, 
    decimal Price, 
    int CategoryId
) : IRequest<Result<ProductDto>>;

// Handler
public class CreateProductCommandHandler 
    : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    private readonly ICatalogDbContext _context;
    private readonly IEventBus _eventBus;
    
    public async Task<Result<ProductDto>> Handle(
        CreateProductCommand request, 
        CancellationToken cancellationToken)
    {
        var product = new Product(
            request.Name, 
            request.Description, 
            request.Price, 
            request.CategoryId
        );
        
        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);
        
        // Publish domain event
        await _eventBus.PublishAsync(
            new ProductCreatedEvent(product.Id, product.Name)
        );
        
        return Result<ProductDto>.Success(product.ToDto());
    }
}
```

### Query Pattern
```csharp
// Query
public record GetProductByIdQuery(int Id) : IRequest<ProductDto>;

// Handler
public class GetProductByIdQueryHandler 
    : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly ICatalogDbContext _context;
    private readonly IDistributedCache _cache;
    
    public async Task<ProductDto> Handle(
        GetProductByIdQuery request, 
        CancellationToken cancellationToken)
    {
        var cacheKey = $"product:{request.Id}";
        
        // Try cache first
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
            return JsonSerializer.Deserialize<ProductDto>(cached);
            
        // Query database
        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == request.Id);
            
        var dto = product.ToDto();
        
        // Cache result
        await _cache.SetStringAsync(
            cacheKey, 
            JsonSerializer.Serialize(dto),
            new DistributedCacheEntryOptions 
            { 
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) 
            }
        );
        
        return dto;
    }
}
```

## Event Bus Implementation

### Kafka Producer
```csharp
public class KafkaEventBus : IEventBus
{
    private readonly IProducer<string, string> _producer;
    
    public async Task PublishAsync<TEvent>(
        TEvent @event, 
        CancellationToken ct = default) 
        where TEvent : IEvent
    {
        var topic = @event.GetType().Name.ToLower();
        var message = JsonSerializer.Serialize(@event);
        
        await _producer.ProduceAsync(
            topic,
            new Message<string, string>
            {
                Key = @event.AggregateId.ToString(),
                Value = message,
                Headers = new Headers
                {
                    { "event-type", Encoding.UTF8.GetBytes(@event.GetType().Name) },
                    { "correlation-id", Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()) }
                }
            },
            ct
        );
    }
}
```

### Kafka Consumer
```csharp
public class KafkaEventConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceProvider _serviceProvider;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(new[] { "order-events" });
        
        while (!stoppingToken.IsCancellationRequested)
        {
            var consumeResult = _consumer.Consume(stoppingToken);
            
            using var scope = _serviceProvider.CreateScope();
            var handler = scope.ServiceProvider
                .GetRequiredService<IEventHandler>();
                
            await handler.HandleAsync(
                consumeResult.Message.Value, 
                stoppingToken
            );
            
            _consumer.Commit(consumeResult);
        }
    }
}
```

## Saga Pattern Implementation

### Saga Orchestrator
```csharp
public class OrderSaga : ISaga<OrderCreatedEvent>
{
    private readonly IEventBus _eventBus;
    private readonly ISagaStateRepository _stateRepository;
    
    public async Task HandleAsync(OrderCreatedEvent @event)
    {
        var state = new SagaState(@event.OrderId, SagaStep.Created);
        await _stateRepository.SaveAsync(state);
        
        try
        {
            // Step 1: Reserve Inventory
            await _eventBus.PublishAsync(new ReserveInventoryCommand(
                @event.OrderId, @event.Items
            ));
            state.TransitionTo(SagaStep.InventoryReserved);
            
            // Step 2: Process Payment
            await _eventBus.PublishAsync(new ProcessPaymentCommand(
                @event.OrderId, @event.TotalAmount
            ));
            state.TransitionTo(SagaStep.PaymentProcessed);
            
            // Step 3: Create Shipment
            await _eventBus.PublishAsync(new CreateShipmentCommand(
                @event.OrderId, @event.ShippingAddress
            ));
            state.TransitionTo(SagaStep.ShipmentCreated);
            
            // Saga completed
            state.Complete();
        }
        catch (Exception ex)
        {
            // Compensating transactions
            await CompensateAsync(state);
            state.Fail(ex.Message);
        }
        finally
        {
            await _stateRepository.SaveAsync(state);
        }
    }
    
    private async Task CompensateAsync(SagaState state)
    {
        if (state.CurrentStep >= SagaStep.InventoryReserved)
            await _eventBus.PublishAsync(
                new ReleaseInventoryCommand(state.OrderId)
            );
            
        if (state.CurrentStep >= SagaStep.PaymentProcessed)
            await _eventBus.PublishAsync(
                new RefundPaymentCommand(state.OrderId)
            );
    }
}
```

## Idempotency Implementation

### Idempotency Key Handler
```csharp
public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    
    public async Task InvokeAsync(
        HttpContext context, 
        IIdempotencyKeyRepository repository)
    {
        if (!context.Request.Headers.TryGetValue(
            "Idempotency-Key", out var key))
        {
            await _next(context);
            return;
        }
        
        var requestHash = await ComputeRequestHashAsync(context.Request);
        var existing = await repository.GetAsync(key);
        
        if (existing != null && existing.RequestHash == requestHash)
        {
            // Return cached response
            context.Response.StatusCode = existing.StatusCode;
            await context.Response.WriteAsync(existing.Response);
            return;
        }
        
        // Capture response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        
        await _next(context);
        
        // Store response
        responseBody.Seek(0, SeekOrigin.Begin);
        var response = await new StreamReader(responseBody).ReadToEndAsync();
        
        await repository.SaveAsync(new IdempotencyKey
        {
            Key = key,
            RequestHash = requestHash,
            Response = response,
            StatusCode = context.Response.StatusCode,
            CreatedAt = DateTime.UtcNow
        });
        
        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);
    }
}
```

## Resilience Policies

### Polly Configuration
```csharp
services.AddHttpClient<ICatalogService, CatalogService>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy())
    .AddPolicyHandler(GetTimeoutPolicy());

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Log.Warning(
                    "Retry {RetryCount} after {Delay}ms", 
                    retryCount, timespan.TotalMilliseconds
                );
            }
        );
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (outcome, duration) =>
            {
                Log.Error("Circuit breaker opened for {Duration}s", duration.TotalSeconds);
            },
            onReset: () =>
            {
                Log.Information("Circuit breaker reset");
            }
        );
}

static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
{
    return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30));
}
```

## Observability Implementation

### OpenTelemetry Setup
```csharp
services.AddOpenTelemetry()
    .WithTracing(builder =>
    {
        builder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddSource("Catalog.API")
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(
                    configuration["OpenTelemetry:OtlpEndpoint"]
                );
            });
    })
    .WithMetrics(builder =>
    {
        builder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddOtlpExporter();
    });
```

### Custom Metrics
```csharp
public class OrderMetrics
{
    private readonly Counter<long> _ordersCreated;
    private readonly Histogram<double> _orderValue;
    
    public OrderMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Order.API");
        _ordersCreated = meter.CreateCounter<long>("orders.created");
        _orderValue = meter.CreateHistogram<double>("orders.value");
    }
    
    public void RecordOrderCreated(decimal amount)
    {
        _ordersCreated.Add(1);
        _orderValue.Record((double)amount);
    }
}
```

## API Gateway (YARP) Configuration

### Route Configuration
```json
{
  "ReverseProxy": {
    "Routes": {
      "identity-route": {
        "ClusterId": "identity-cluster",
        "Match": {
          "Path": "/api/identity/{**catch-all}"
        },
        "Transforms": [
          { "PathPattern": "/api/{**catch-all}" }
        ]
      },
      "catalog-route": {
        "ClusterId": "catalog-cluster",
        "Match": {
          "Path": "/api/catalog/{**catch-all}"
        },
        "Transforms": [
          { "PathPattern": "/api/{**catch-all}" }
        ]
      }
    },
    "Clusters": {
      "identity-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://identity-api:80"
          }
        }
      },
      "catalog-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://catalog-api:80"
          }
        },
        "HealthCheck": {
          "Active": {
            "Enabled": true,
            "Interval": "00:00:30",
            "Path": "/health"
          }
        }
      }
    }
  }
}
```

### Rate Limiting (.NET 9)
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });
    
    options.AddTokenBucketLimiter("token", opt =>
    {
        opt.TokenLimit = 1000;
        opt.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
        opt.TokensPerPeriod = 100;
    });
});

app.UseRateLimiter();
```

## Testing Strategy

### Unit Test Example
```csharp
public class CreateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesProduct()
    {
        // Arrange
        var fixture = new Fixture();
        var command = fixture.Create<CreateProductCommand>();
        var dbContext = new Mock<ICatalogDbContext>();
        var eventBus = new Mock<IEventBus>();
        
        var handler = new CreateProductCommandHandler(
            dbContext.Object, 
            eventBus.Object
        );
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), 
            Times.Once);
        eventBus.Verify(x => x.PublishAsync(
            It.IsAny<ProductCreatedEvent>(), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}
```

### Integration Test Example
```csharp
public class CatalogApiTests : IAsyncLifetime
{
    private PostgreSqlContainer _dbContainer;
    private WebApplicationFactory<Program> _factory;
    
    public async Task InitializeAsync()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithDatabase("catalog_test")
            .Build();
            
        await _dbContainer.StartAsync();
        
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace DbContext with test container
                    services.AddDbContext<CatalogDbContext>(options =>
                        options.UseNpgsql(_dbContainer.GetConnectionString())
                    );
                });
            });
    }
    
    [Fact]
    public async Task CreateProduct_ReturnsCreatedProduct()
    {
        var client = _factory.CreateClient();
        var command = new CreateProductCommand(
            "Test Product", "Description", 99.99m, 1
        );
        
        var response = await client.PostAsJsonAsync(
            "/api/products", command
        );
        
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        product.Should().NotBeNull();
        product!.Name.Should().Be("Test Product");
    }
    
    public async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await _factory.DisposeAsync();
    }
}
```

## Performance Optimization

### Database Optimizations
- Indexes on frequently queried columns
- Compiled queries for hot paths
- Pagination for list endpoints
- Read replicas for read-heavy services
- Connection pooling (default in EF Core)

### Caching Strategy
- Redis for distributed cache
- Response caching for GET endpoints
- Cache-aside pattern implementation
- TTL based on data volatility

### Async Patterns
- async/await throughout
- ValueTask for hot paths
- IAsyncEnumerable for streaming
- Channels for producer-consumer patterns

---

**Version**: .NET 9.0 | **Last Updated**: 2024
