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

Each service follows 4-layer architecture with shared BuildingBlocks:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                       CLEAN ARCHITECTURE                                │
│                  (Each Microservice Structure)                          │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│  Service.API/  (Presentation Layer - Port 8080)                         │
├─────────────────────────────────────────────────────────────────────────┤
│  Controllers/                    # REST API Endpoints                   │
│    ├── ProductsController.cs    # HTTP: GET, POST, PUT, DELETE          │
│    └── CategoriesController.cs  # Minimal APIs also supported           │
│                                                                         │
│  Middleware/                     # Request Pipeline                     │
│    ├── ExceptionHandlerMiddleware.cs                                    │
│    ├── CorrelationIdMiddleware.cs                                       │
│    └── RequestLoggingMiddleware.cs                                      │
│                                                                         │
│  Extensions/                     # DI & Configuration                   │
│    ├── ServiceCollectionExtensions.cs                                   │
│    └── ApplicationBuilderExtensions.cs                                  │
│                                                                         │
│  Program.cs                      # Entry Point                          │
│    • Configure services (DI)                                            │
│    • Setup middleware pipeline                                          │
│    • Configure OpenTelemetry, Serilog                                   │
│    • Register BuildingBlocks                                            │
│                                                                         │
│  Dependencies: ↓ Application Layer, BuildingBlocks                      │
└─────────────────────────────────────────────────────────────────────────┘
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│  Service.Application/  (Application Layer - Business Logic)             │
├─────────────────────────────────────────────────────────────────────────┤
│  Commands/  (Write Operations - CQRS)                                   │
│    ├── CreateProductCommand.cs          # Record/DTO                    │
│    ├── CreateProductCommandHandler.cs   # MediatR IRequestHandler       │
│    └── CreateProductCommandValidator.cs # FluentValidation              │
│                                                                         │
│  Queries/  (Read Operations - CQRS)                                     │
│    ├── GetProductByIdQuery.cs           # Record/DTO                    │
│    ├── GetProductByIdQueryHandler.cs    # MediatR IRequestHandler       │
│    └── GetProductsQuery.cs              # With pagination               │
│                                                                         │
│  DTOs/  (Data Transfer Objects)                                         │
│    ├── ProductDto.cs                                                    │
│    ├── CategoryDto.cs                                                   │
│    └── PaginatedResult<T>.cs                                            │
│                                                                         │
│  Mappings/  (AutoMapper Profiles)                                       │
│    └── ProductMappingProfile.cs  # Entity ↔ DTO mappings                │
│                                                                         │
│  Validators/  (FluentValidation Rules)                                  │
│    ├── CreateProductValidator.cs                                        │
│    └── UpdateProductValidator.cs                                        │
│                                                                         │
│  Services/  (Application Services)                                      │
│    └── ProductApplicationService.cs                                     │
│                                                                         │
│  EventHandlers/  (Domain Event Handlers)                                │
│    ├── ProductCreatedEventHandler.cs    # Publish to Kafka              │
│    └── PriceChangedEventHandler.cs      # Cache invalidation            │
│                                                                         │
│  Sagas/  (Only in Order Service)                                        │
│    ├── OrderCreationSaga.cs             # Saga Orchestrator             │
│    ├── OrderCreationSagaState.cs        # State management              │
│    └── Steps/                           # Individual saga steps         │
│        ├── ReserveInventoryStep.cs                                      │
│        ├── ProcessPaymentStep.cs                                        │
│        └── ConfirmOrderStep.cs                                          │
│                                                                         │
│  Dependencies: ↓ Domain Layer, BuildingBlocks.Application               │
└─────────────────────────────────────────────────────────────────────────┘
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│  Service.Domain/  (Domain Layer - Core Business Rules)                  │
├─────────────────────────────────────────────────────────────────────────┤
│  Aggregates/  (Aggregate Roots)                                         │
│    └── Product.cs  (Root Entity with invariants)                        │
│                                                                         │
│  Entities/  (Domain Entities)                                           │
│    ├── Category.cs                                                      │
│    └── ProductImage.cs                                                  │
│                                                                         │
│  ValueObjects/  (Immutable Value Objects)                               │
│    ├── Money.cs                                                         │
│    ├── Address.cs                                                       │
│    └── Email.cs                                                         │
│                                                                         │
│  Events/  (Domain Events)                                               │
│    ├── ProductCreatedEvent.cs           # Raised on creation            │
│    ├── PriceChangedEvent.cs             # Raised on price update        │
│    └── ProductDeletedEvent.cs                                           │
│                                                                         │
│  Exceptions/  (Domain Exceptions)                                       │
│    ├── ProductNotFoundException.cs                                      │
│    └── InvalidPriceException.cs                                         │
│                                                                         │
│  Interfaces/  (Repository Contracts)                                    │
│    ├── IProductRepository.cs                                            │
│    └── IUnitOfWork.cs                                                   │
│                                                                         │
│  Dependencies: ✓ NONE (Pure domain, no external dependencies)           │
└─────────────────────────────────────────────────────────────────────────┘
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│  Service.Infrastructure/  (Infrastructure Layer - External Concerns)    │
├─────────────────────────────────────────────────────────────────────────┤
│  Persistence/                                                           │
│    ├── CatalogDbContext.cs              # EF Core DbContext             │
│    ├── Configurations/                  # Entity Type Configurations    │
│    │   ├── ProductConfiguration.cs      # Fluent API mappings           │
│    │   └── CategoryConfiguration.cs                                     │
│    ├── Migrations/                      # EF Core Migrations            │
│    │   └── 20241001_InitialCreate.cs                                    │
│    └── Repositories/                    # Repository Implementations    │
│        ├── ProductRepository.cs         # Implements IProductRepository │
│        └── UnitOfWork.cs                # Transaction management        │
│                                                                         │
│  EventBus/  (Kafka Integration)                                         │
│    ├── KafkaProducer.cs                 # Publish events                │
│    ├── KafkaConsumer.cs                 # Consume events                │
│    └── OutboxProcessor.cs               # Outbox pattern worker         │
│                                                                         │
│  Caching/  (Redis Integration)                                          │
│    ├── RedisCacheService.cs             # IDistributedCache wrapper     │
│    └── CacheKeyGenerator.cs                                             │
│                                                                         │
│  ExternalServices/  (3rd Party APIs)                                    │
│    └── PaymentGatewayClient.cs          # External payment API          │
│                                                                         │
│  BackgroundServices/  (Hosted Services)                                 │
│    ├── OutboxPublisher.cs               # Publishes outbox messages     │
│    └── SagaRecoveryService.cs           # Order Service only            │
│                                                                         │
│  Dependencies: ↑ Domain, Application, BuildingBlocks.Infrastructure     │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│  BuildingBlocks/  (Shared Libraries - Cross-Cutting Concerns)           │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  BuildingBlocks.EventBus/         # Kafka abstraction layer             │
│    ├── IEventBus.cs               # Publish/Subscribe interface         │
│    ├── IEvent.cs                  # Base event interface                │
│    └── EventBusExtensions.cs      # DI registration                     │
│                                                                         │
│  BuildingBlocks.Saga/             # Saga orchestration framework        │
│    ├── ISaga.cs                   # Saga interface                      │
│    ├── ISagaStep.cs               # Individual step interface           │
│    ├── SagaOrchestrator.cs        # Execute & compensate logic          │
│    └── ISagaStateRepository.cs    # Persist saga state                  │
│                                                                         │
│  BuildingBlocks.Resilience/       # Polly policies                      │
│    ├── ResiliencePolicies.cs      # Retry, Circuit Breaker, Timeout     │
│    └── HttpClientExtensions.cs    # AddResilientHttpClient()            │
│                                                                         │
│  BuildingBlocks.Idempotency/      # Idempotent request handling         │
│    ├── IdempotencyMiddleware.cs   # HTTP middleware                     │
│    └── IIdempotencyKeyRepository.cs                                     │
│                                                                         │
│  BuildingBlocks.Observability/    # OpenTelemetry setup                 │
│    ├── ObservabilityExtensions.cs # Tracing, Metrics, Logging           │
│    └── CorrelationIdMiddleware.cs # Request correlation                 │
│                                                                         │
│  BuildingBlocks.Authentication/   # JWT handling                        │
│    ├── JwtConfiguration.cs        # Token generation/validation         │
│    └── AuthenticationExtensions.cs                                      │
│                                                                         │
│  BuildingBlocks.Domain/           # Shared domain primitives            │
│    ├── Entity.cs                  # Base entity class                   │
│    ├── ValueObject.cs             # Base value object                   │
│    ├── AggregateRoot.cs           # Base aggregate root                 │
│    └── DomainEvent.cs             # Base domain event                   │
│                                                                         │
│  BuildingBlocks.Application/      # Shared application utilities        │
│    ├── Result.cs                  # Result pattern (success/failure)    │
│    ├── PagedList.cs               # Pagination helper                   │
│    └── MediatRBehaviors/          # Pipeline behaviors                  │
│        ├── ValidationBehavior.cs  # FluentValidation pipeline           │
│        └── LoggingBehavior.cs     # Request/response logging            │
│                                                                         │
│  BuildingBlocks.Infrastructure/   # Shared infrastructure utilities     │
│    ├── EntityFrameworkExtensions.cs                                     │
│    └── OutboxPattern/             # Transactional outbox                │
│        ├── OutboxMessage.cs                                             │
│        └── IOutboxRepository.cs                                         │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘

DEPENDENCY FLOW (Dependency Inversion Principle):
═══════════════════════════════════════════════════

  API Layer
     ↓ (depends on)
  Application Layer
     ↓ (depends on)
  Domain Layer  ◄──────────┐
     ↑ (implemented by)    │ (both depend on domain abstractions)
  Infrastructure Layer ────┘

  All layers can use BuildingBlocks (cross-cutting concerns)
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

#### Identity Service Database (identity_db)

```
┌──────────────────────────────────────────────────────────────┐
│  users                                                       │
├──────────────────────────────────────────────────────────────┤
│  PK  id                  UUID                                │
│      email               VARCHAR(255)  UNIQUE NOT NULL       │
│      password_hash       VARCHAR(512)  NOT NULL              │
│      first_name          VARCHAR(100)                        │
│      last_name           VARCHAR(100)                        │
│      role                VARCHAR(50)   NOT NULL              │
│      is_active           BOOLEAN       DEFAULT TRUE          │
│      email_confirmed     BOOLEAN       DEFAULT FALSE         │
│      created_at          TIMESTAMP     NOT NULL              │
│      updated_at          TIMESTAMP                           │
│                                                              │
│  INDEXES:                                                    │
│    idx_users_email ON (email)                                │
│    idx_users_role ON (role)                                  │
└──────────────────────────────────────────────────────────────┘
                    │
                    │ 1
                    │
                    │ *
┌──────────────────────────────────────────────────────────────┐
│  refresh_tokens                                              │
├──────────────────────────────────────────────────────────────┤
│  PK  id                  UUID                                │
│  FK  user_id             UUID          → users(id)           │
│      token               VARCHAR(512)  UNIQUE NOT NULL       │
│      expires_at          TIMESTAMP     NOT NULL              │
│      created_at          TIMESTAMP     NOT NULL              │
│      revoked_at          TIMESTAMP                           │
│      is_revoked          BOOLEAN       DEFAULT FALSE         │
│                                                              │
│  INDEXES:                                                    │
│    idx_refresh_tokens_user_id ON (user_id)                   │
│    idx_refresh_tokens_token ON (token)                       │
└──────────────────────────────────────────────────────────────┘

#### Catalog Service Database (catalog_db)

```
┌──────────────────────────────────────────────────────────────┐
│  categories                                                  │
├──────────────────────────────────────────────────────────────┤
│  PK  id                  INT           IDENTITY              │
│      name                VARCHAR(200)  NOT NULL              │
│      description         TEXT                                │
│  FK  parent_id           INT           → categories(id) NULL │
│      created_at          TIMESTAMP     NOT NULL              │
│      updated_at          TIMESTAMP                           │
│                                                              │
│  INDEXES:                                                    │
│    idx_categories_name ON (name)                             │
│    idx_categories_parent_id ON (parent_id)                   │
└──────────────────────────────────────────────────────────────┘
                    │
                    │ 1
                    │
                    │ *
┌──────────────────────────────────────────────────────────────┐
│  products                                                    │
├──────────────────────────────────────────────────────────────┤
│  PK  id                  INT           IDENTITY              │
│      name                VARCHAR(200)  NOT NULL              │
│      description         TEXT                                │
│      price               DECIMAL(18,2) NOT NULL              │
│  FK  category_id         INT           → categories(id)      │
│      sku                 VARCHAR(50)   UNIQUE NOT NULL       │
│      is_active           BOOLEAN       DEFAULT TRUE          │
│      created_at          TIMESTAMP     NOT NULL              │
│      updated_at          TIMESTAMP                           │
│                                                              │
│  INDEXES:                                                    │
│    idx_products_name ON (name)                               │
│    idx_products_category_id ON (category_id)                 │
│    idx_products_sku ON (sku)                                 │
│    idx_products_price ON (price)                             │
└──────────────────────────────────────────────────────────────┘
                    │
                    │ 1
                    │
                    │ *
┌──────────────────────────────────────────────────────────────┐
│  product_images                                              │
├──────────────────────────────────────────────────────────────┤
│  PK  id                  INT           IDENTITY              │
│  FK  product_id          INT           → products(id)        │
│      url                 VARCHAR(500)  NOT NULL              │
│      is_primary          BOOLEAN       DEFAULT FALSE         │
│      display_order       INT           DEFAULT 0             │
│      created_at          TIMESTAMP     NOT NULL              │
│                                                              │
│  INDEXES:                                                    │
│    idx_product_images_product_id ON (product_id)             │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│  outbox_messages  (Transactional Outbox Pattern)             │
├──────────────────────────────────────────────────────────────┤
│  PK  id                  UUID                                │
│      event_type          VARCHAR(100)  NOT NULL              │
│      aggregate_id        VARCHAR(100)  NOT NULL              │
│      payload             JSONB         NOT NULL              │
│      created_at          TIMESTAMP     NOT NULL              │
│      processed_at        TIMESTAMP                           │
│      is_processed        BOOLEAN       DEFAULT FALSE         │
│      retry_count         INT           DEFAULT 0             │
│                                                              │
│  INDEXES:                                                    │
│   idx_outbox_messages_processed ON (is_processed, created_at)│
└──────────────────────────────────────────────────────────────┘

#### Order Service Database (order_db)

```
┌──────────────────────────────────────────────────────────────┐
│  orders                                                      │
├──────────────────────────────────────────────────────────────┤
│  PK  id                  UUID                                │
│      user_id             UUID          NOT NULL              │
│      status              VARCHAR(50)   NOT NULL              │
│      total_amount        DECIMAL(18,2) NOT NULL              │
│      shipping_address    JSONB         NOT NULL              │
│      billing_address     JSONB         NOT NULL              │
│      created_at          TIMESTAMP     NOT NULL              │
│      updated_at          TIMESTAMP                           │
│      confirmed_at        TIMESTAMP                           │
│      cancelled_at        TIMESTAMP                           │
│                                                              │
│  INDEXES:                                                    │
│    idx_orders_user_id ON (user_id)                           │
│    idx_orders_status ON (status)                             │
│    idx_orders_created_at ON (created_at DESC)                │
└──────────────────────────────────────────────────────────────┘
                    │
                    │ 1
                    │
                    │ *
┌──────────────────────────────────────────────────────────────┐
│  order_items                                                 │
├──────────────────────────────────────────────────────────────┤
│  PK  id                  UUID                                │
│  FK  order_id            UUID          → orders(id)          │
│      product_id          INT           NOT NULL              │
│      product_name        VARCHAR(200)  NOT NULL              │
│      quantity            INT           NOT NULL              │
│      unit_price          DECIMAL(18,2) NOT NULL              │
│      total_price         DECIMAL(18,2) NOT NULL              │
│                                                              │
│  INDEXES:                                                    │
│    idx_order_items_order_id ON (order_id)                    │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│  saga_state  (Saga Orchestration State)                      │
├──────────────────────────────────────────────────────────────┤
│  PK  id                  UUID                                │
│      correlation_id      UUID          UNIQUE NOT NULL       │
│  FK  order_id            UUID          → orders(id)          │
│      current_step        VARCHAR(100)  NOT NULL              │
│      status              VARCHAR(50)   NOT NULL              │
│      state_data          JSONB         NOT NULL              │
│      error_message       TEXT                                │
│      created_at          TIMESTAMP     NOT NULL              │
│      updated_at          TIMESTAMP                           │
│      completed_at        TIMESTAMP                           │
│                                                              │
│  INDEXES:                                                    │
│    idx_saga_state_correlation_id ON (correlation_id)         │
│    idx_saga_state_order_id ON (order_id)                     │
│    idx_saga_state_status ON (status)                         │
└──────────────────────────────────────────────────────────────┘

#### Payment Service Database (payment_db)

```
┌──────────────────────────────────────────────────────────────┐
│  payments                                                    │
├──────────────────────────────────────────────────────────────┤
│  PK  id                  UUID                                │
│      order_id            UUID          UNIQUE NOT NULL       │
│      amount              DECIMAL(18,2) NOT NULL              │
│      currency            VARCHAR(3)    DEFAULT 'USD'         │
│      status              VARCHAR(50)   NOT NULL              │
│      gateway             VARCHAR(50)   NOT NULL              │
│      transaction_id      VARCHAR(200)  UNIQUE                │
│      created_at          TIMESTAMP     NOT NULL              │
│      processed_at        TIMESTAMP                           │
│      failed_at           TIMESTAMP                           │
│      error_message       TEXT                                │
│                                                              │
│  INDEXES:                                                    │
│    idx_payments_order_id ON (order_id)                       │
│    idx_payments_status ON (status)                           │
│    idx_payments_transaction_id ON (transaction_id)           │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│  idempotency_keys  (Idempotent Request Handling)             │
├──────────────────────────────────────────────────────────────┤
│  PK  key                 VARCHAR(100)                        │
│      request_hash        VARCHAR(64)   NOT NULL              │
│      response            TEXT          NOT NULL              │
│      status_code         INT           NOT NULL              │
│      created_at          TIMESTAMP     NOT NULL              │
│      expires_at          TIMESTAMP     NOT NULL              │
│                                                              │
│  INDEXES:                                                    │
│    idx_idempotency_keys_expires_at ON (expires_at)           │
└──────────────────────────────────────────────────────────────┘

#### Inventory Service Database (inventory_db)

```
┌──────────────────────────────────────────────────────────────┐
│  warehouses                                                  │
├──────────────────────────────────────────────────────────────┤
│  PK  id                  INT           IDENTITY              │
│      name                VARCHAR(200)  NOT NULL              │
│      location            VARCHAR(500)  NOT NULL              │
│      is_active           BOOLEAN       DEFAULT TRUE          │
│      created_at          TIMESTAMP     NOT NULL              │
└──────────────────────────────────────────────────────────────┘
                    │
                    │ 1
                    │
                    │ *
┌──────────────────────────────────────────────────────────────┐
│  inventory                                                   │
├──────────────────────────────────────────────────────────────┤
│  PK  id                  UUID                                │
│      product_id          INT           NOT NULL              │
│  FK  warehouse_id        INT           → warehouses(id)      │
│      quantity            INT           NOT NULL              │
│      reserved_quantity   INT           DEFAULT 0             │
│      available_quantity  INT COMPUTED  (quantity - reserved) │
│      updated_at          TIMESTAMP     NOT NULL              │
│                                                              │
│  UNIQUE: (product_id, warehouse_id)                          │
│  INDEXES:                                                    │
│    idx_inventory_product_id ON (product_id)                  │
│    idx_inventory_warehouse_id ON (warehouse_id)              │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│  reservations  (Temporary Stock Reservations)                │
├──────────────────────────────────────────────────────────────┤
│  PK  id                  UUID                                │
│      product_id          INT           NOT NULL              │
│      order_id            UUID          NOT NULL              │
│      quantity            INT           NOT NULL              │
│      status              VARCHAR(50)   NOT NULL              │
│      created_at          TIMESTAMP     NOT NULL              │
│      expires_at          TIMESTAMP     NOT NULL              │
│      released_at         TIMESTAMP                           │
│                                                              │
│  INDEXES:                                                    │
│    idx_reservations_product_id ON (product_id)               │
│    idx_reservations_order_id ON (order_id)                   │
│    idx_reservations_expires_at ON (expires_at)               │
└──────────────────────────────────────────────────────────────┘

#### Shipping Service Database (shipping_db)

```
┌──────────────────────────────────────────────────────────────┐
│  shipments                                                   │
├──────────────────────────────────────────────────────────────┤
│  PK  id                  UUID                                │
│      order_id            UUID          UNIQUE NOT NULL       │
│      tracking_number     VARCHAR(100)  UNIQUE NOT NULL       │
│      carrier             VARCHAR(100)  NOT NULL              │
│      status              VARCHAR(50)   NOT NULL              │
│      shipping_address    JSONB         NOT NULL              │
│      estimated_delivery  DATE                                │
│      actual_delivery     DATE                                │
│      created_at          TIMESTAMP     NOT NULL              │
│      updated_at          TIMESTAMP                           │
│                                                              │
│  INDEXES:                                                    │
│    idx_shipments_order_id ON (order_id)                      │
│    idx_shipments_tracking_number ON (tracking_number)        │
│    idx_shipments_status ON (status)                          │
└──────────────────────────────────────────────────────────────┘
                    │
                    │ 1
                    │
                    │ *
┌──────────────────────────────────────────────────────────────┐
│  tracking_events                                             │
├──────────────────────────────────────────────────────────────┤
│  PK  id                  UUID                                │
│  FK  shipment_id         UUID          → shipments(id)       │
│      status              VARCHAR(50)   NOT NULL              │
│      location            VARCHAR(200)                        │
│      description         TEXT                                │
│      timestamp           TIMESTAMP     NOT NULL              │
│                                                              │
│  INDEXES:                                                    │
│    idx_tracking_events_shipment_id ON (shipment_id)          │
│    idx_tracking_events_timestamp ON (timestamp DESC)         │
└──────────────────────────────────────────────────────────────┘

#### Notification Service Database (notification_db)

```
┌──────────────────────────────────────────────────────────────┐
│  templates                                                   │
├──────────────────────────────────────────────────────────────┤
│  PK  id                  INT           IDENTITY              │
│      name                VARCHAR(100)  UNIQUE NOT NULL       │
│      type                VARCHAR(50)   NOT NULL (Email/SMS)  │
│      subject             VARCHAR(200)                        │
│      body                TEXT          NOT NULL              │
│      is_active           BOOLEAN       DEFAULT TRUE          │
│      created_at          TIMESTAMP     NOT NULL              │
│      updated_at          TIMESTAMP                           │
│                                                              │
│  INDEXES:                                                    │
│    idx_templates_name ON (name)                              │
│    idx_templates_type ON (type)                              │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│  notifications                                               │
├──────────────────────────────────────────────────────────────┤
│  PK  id                  UUID                                │
│      user_id             UUID          NOT NULL              │
│      type                VARCHAR(50)   NOT NULL              │
│  FK  template_id         INT           → templates(id)       │
│      recipient           VARCHAR(255)  NOT NULL              │
│      status              VARCHAR(50)   NOT NULL              │
│      sent_at             TIMESTAMP                           │
│      failed_at           TIMESTAMP                           │
│      error_message       TEXT                                │
│      created_at          TIMESTAMP     NOT NULL              │
│                                                              │
│  INDEXES:                                                    │
│    idx_notifications_user_id ON (user_id)                    │
│    idx_notifications_status ON (status)                      │
│    idx_notifications_created_at ON (created_at DESC)         │
└──────────────────────────────────────────────────────────────┘

#### Analytics Service Database (analytics_db)

```
┌──────────────────────────────────────────────────────────────┐
│  events  (Raw Event Stream)                                  │
├──────────────────────────────────────────────────────────────┤
│  PK  id                  UUID                                │
│      event_type          VARCHAR(100)  NOT NULL              │
│      user_id             UUID                                │
│      aggregate_id        VARCHAR(100)                        │
│      data                JSONB         NOT NULL              │
│      timestamp           TIMESTAMP     NOT NULL              │
│                                                              │
│  INDEXES:                                                    │
│    idx_events_event_type ON (event_type)                     │
│    idx_events_user_id ON (user_id)                           │
│    idx_events_timestamp ON (timestamp DESC)                  │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│  aggregated_metrics  (Pre-computed Metrics)                  │
├──────────────────────────────────────────────────────────────┤
│  PK  id                  UUID                                │
│      metric_name         VARCHAR(100)  NOT NULL              │
│      value               DECIMAL(18,4) NOT NULL              │
│      period              VARCHAR(20)   NOT NULL (hour/day)   │
│      dimensions          JSONB                               │
│      timestamp           TIMESTAMP     NOT NULL              │
│                                                              │
│  INDEXES:                                                    │
│    idx_metrics_name_period ON (metric_name, period)          │
│    idx_metrics_timestamp ON (timestamp DESC)                 │
└──────────────────────────────────────────────────────────────┘
```

**Database Design Principles**:
- **Each service has its own database** (Database-per-Service pattern)
- **Outbox tables** for reliable event publishing (Transactional Outbox)
- **Proper indexing** on frequently queried columns
- **Timestamps** for audit trail
- **JSONB** for flexible schema (addresses, metadata)
- **Computed columns** for derived values
- **Foreign keys** for referential integrity within service boundaries
- **UUID** for distributed ID generation (except catalog which uses INT)

## CQRS Implementation

### CQRS Pattern Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│              CQRS (Command Query Responsibility Segregation)            │
│                    Using MediatR + EF Core + Redis                      │
└─────────────────────────────────────────────────────────────────────────┘

╔═══════════════════════════════════════════════════════════════════════╗
║                        COMMAND FLOW (Write)                           ║
╚═══════════════════════════════════════════════════════════════════════╝

  HTTP Request (POST /api/products)
        │
        ▼
  ┌───────────────────┐
  │   Controller      │
  └─────────┬─────────┘
            │ CreateProductCommand
            ▼
  ┌────────────────────────────────────────────────────────────────┐
  │                    MediatR Pipeline                            │
  ├────────────────────────────────────────────────────────────────┤
  │                                                                │
  │  1. LoggingBehavior                                            │
  │     ├─→ Log request details                                    │
  │     └─→ Start correlation tracking                             │
  │                                                                │
  │  2. ValidationBehavior                                         │
  │     ├─→ FluentValidation rules                                 │
  │     ├─→ Validate: Name, Price, CategoryId                      │
  │     └─→ Throw ValidationException if invalid                   │
  │                                                                │
  │  3. TransactionBehavior (optional)                             │
  │     └─→ Wrap in database transaction                           │
  │                                                                │
  └────────────────────────────┬───────────────────────────────────┘
                               │
                               ▼
           ┌────────────────────────────────────┐
           │  CreateProductCommandHandler       │
           ├────────────────────────────────────┤
           │  1. Create Domain Entity           │
           │     var product = new Product(...) │
           │                                    │
           │  2. Add to DbContext               │
           │     _context.Products.Add(product) │
           │                                    │
           │  3. Save to Database               │
           │     await SaveChangesAsync()       │
           │                                    │
           │  4. Raise Domain Event             │
           │     ProductCreatedEvent            │
           │                                    │
           │  5. Save to Outbox Table           │
           │     (Transactional Outbox)         │
           │                                    │
           │  6. Return Result<ProductDto>      │
           └────────────────┬───────────────────┘
                            │
            ┌───────────────┴────────────────┐
            │                                │
            ▼                                ▼
  ┌──────────────────┐          ┌─────────────────────┐
  │  PostgreSQL DB   │          │  Outbox Table       │
  │  + products      │          │  + event payload    │
  └──────────────────┘          └─────────┬───────────┘
                                          │
                                          ▼
                              ┌─────────────────────────┐
                              │  Background Worker      │
                              │  (OutboxPublisher)      │
                              ├─────────────────────────┤
                              │  Poll every 5 seconds   │
                              │  Publish to Kafka       │
                              │  Mark as processed      │
                              └─────────────────────────┘
                                          │
                                          ▼
                              ┌─────────────────────────┐
                              │  Kafka Topic:           │
                              │  catalog-events         │
                              └─────────────────────────┘

╔═══════════════════════════════════════════════════════════════════════╗
║                        QUERY FLOW (Read)                              ║
╚═══════════════════════════════════════════════════════════════════════╝

  HTTP Request (GET /api/products/123)
        │
        ▼
  ┌───────────────────┐
  │   Controller      │
  └─────────┬─────────┘
            │ GetProductByIdQuery
            ▼
  ┌────────────────────────────────────────────────────────────────┐
  │                    MediatR Pipeline                            │
  ├────────────────────────────────────────────────────────────────┤
  │  1. LoggingBehavior (Read)                                     │
  │  2. CachingBehavior (Optional)                                 │
  │     ├─→ Check Redis cache first                                │
  │     └─→ Return cached if exists                                │
  └────────────────────────────┬───────────────────────────────────┘
                               │
                               ▼
           ┌────────────────────────────────────┐
           │  GetProductByIdQueryHandler        │
           ├────────────────────────────────────┤
           │  1. Check Redis Cache              │
           │     var cacheKey = "product:123"   │
           │     var cached = await GetAsync()  │
           │     if (cached != null)            │
           │         return cached;             │
           │                                    │
           │  2. Query Database (if cache miss) │
           │     await _context.Products        │
           │         .Include(p => p.Category)  │
           │         .AsNoTracking()            │
           │         .FirstOrDefaultAsync()     │
           │                                    │
           │  3. Map to DTO                     │
           │     var dto = product.ToDto();     │
           │                                    │
           │  4. Store in Cache                 │
           │     await SetAsync(cacheKey, dto,  │
           │         expiry: 10 minutes)        │
           │                                    │
           │  5. Return ProductDto              │
           └────────────────┬───────────────────┘
                            │
            ┌───────────────┴────────────────┐
            │                                │
            ▼                                ▼
  ┌──────────────────┐          ┌─────────────────────┐
  │  PostgreSQL DB   │          │  Redis Cache        │
  │  (Read Replica)  │          │  TTL: 10 minutes    │
  │  .AsNoTracking() │          │  Invalidate on      │
  │                  │          │  ProductUpdated     │
  └──────────────────┘          └─────────────────────┘

╔═══════════════════════════════════════════════════════════════════════╗
║                     CACHE INVALIDATION FLOW                           ║
╚═══════════════════════════════════════════════════════════════════════╝

  ProductUpdatedEvent (from Kafka)
        │
        ▼
  ┌─────────────────────────────────┐
  │  ProductUpdatedEventHandler     │
  ├─────────────────────────────────┤
  │  await _cache.RemoveAsync(      │
  │      $"product:{productId}")    │
  │                                 │
  │  # Next read will fetch from DB │
  │  # and repopulate cache         │
  └─────────────────────────────────┘

╔═══════════════════════════════════════════════════════════════════════╗
║                   BENEFITS OF THIS APPROACH                           ║
╚═══════════════════════════════════════════════════════════════════════╝

  Commands (Write):
    • Validated before processing
    • Domain events published reliably (Outbox)
    • Transactional consistency
    • Audit trail via events

  Queries (Read):
    • Optimized for read performance
    • No tracking overhead (.AsNoTracking())
    • Redis caching for hot data
    • Can use read replicas
    • Separate scaling from writes
```

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

### Event-Driven Architecture with Kafka

```
┌─────────────────────────────────────────────────────────────────────────┐
│                  KAFKA EVENT BUS ARCHITECTURE                           │
│              (Transactional Outbox + At-Least-Once Delivery)            │
└─────────────────────────────────────────────────────────────────────────┘

╔═══════════════════════════════════════════════════════════════════════╗
║                        PRODUCER SIDE (Publisher)                      ║
╚═══════════════════════════════════════════════════════════════════════╝

┌─────────────────────────────────────────────────────────────────────┐
│  Service (e.g., Catalog Service)                                    │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  1. Business Logic Executes                                         │
│     ┌────────────────────────────┐                                  │
│     │ CreateProductCommandHandler│                                  │
│     └──────────┬─────────────────┘                                  │
│                │                                                    │
│  2. Domain Event Raised                                             │
│                ▼                                                    │
│     ┌────────────────────────────┐                                  │
│     │  ProductCreatedEvent       │                                  │
│     │  • ProductId               │                                  │
│     │  • Name, Price             │                                  │
│     │  • Timestamp               │                                  │
│     └──────────┬─────────────────┘                                  │
│                │                                                    │
│  3. Database Transaction (ACID)                                     │
│                ▼                                                    │
│     ┌─────────────────────────────────────────────┐                 │
│     │   PostgreSQL Transaction                    │                 │
│     ├─────────────────────────────────────────────┤                 │
│     │  BEGIN TRANSACTION;                         │                 │
│     │                                             │                 │
│     │  -- Insert product                          │                 │
│     │  INSERT INTO products VALUES (...);         │                 │
│     │                                             │                 │
│     │  -- Insert event into outbox (same txn!)    │                 │
│     │  INSERT INTO outbox_messages (              │                 │
│     │    id,                                      │                 │
│     │    event_type,                              │                 │
│     │    aggregate_id,                            │                 │
│     │    payload,                                 │                 │
│     │    created_at,                              │                 │
│     │    is_processed                             │                 │
│     │  ) VALUES (                                 │                 │
│     │    'uuid',                                  │                 │
│     │    'ProductCreatedEvent',                   │                 │
│     │    '123',                                   │                 │
│     │    '{"productId": 123, ...}',               │                 │
│     │    NOW(),                                   │                 │
│     │    FALSE                                    │                 │
│     │  );                                         │                 │
│     │                                             │                 │
│     │  COMMIT;  ✓                                 │                 │
│     └─────────────────┬───────────────────────────┘                 │
│                       │                                             │
└───────────────────────┼─────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────────────┐
│  OutboxPublisher (Background Service)                               │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Polling Interval: 5 seconds                                        │
│  Batch Size: 100 messages                                           │
│                                                                     │
│  while (true)                                                       │
│  {                                                                  │
│      ┌──────────────────────────────────┐                           │
│      │ 1. Query Unprocessed Messages    │                           │
│      │    SELECT * FROM outbox_messages │                           │
│      │    WHERE is_processed = FALSE    │                           │
│      │    ORDER BY created_at           │                           │
│      │    LIMIT 100;                    │                           │
│      └──────────┬───────────────────────┘                           │
│                 │                                                   │
│                 ▼                                                   │
│      ┌──────────────────────────────────┐                           │
│      │ 2. Publish to Kafka              │                           │
│      │    foreach (msg in messages)     │                           │
│      │    {                             │                           │
│      │      await ProduceAsync(         │                           │
│      │        topic: msg.EventType,     │                           │
│      │        key: msg.AggregateId,     │                           │
│      │        value: msg.Payload,       │                           │
│      │        headers: {                │                           │
│      │          "correlation-id",       │                           │
│      │          "timestamp"             │                           │
│      │        }                         │                           │
│      │      );                          │                           │
│      │    }                             │                           │
│      └──────────┬───────────────────────┘                           │
│                 │                                                   │
│                 ▼                                                   │
│      ┌──────────────────────────────────┐                           │
│      │ 3. Mark as Processed             │                           │
│      │    UPDATE outbox_messages        │                           │
│      │    SET is_processed = TRUE,      │                           │
│      │        processed_at = NOW()      │                           │
│      │    WHERE id IN (...);            │                           │
│      └──────────────────────────────────┘                           │
│                                                                     │
│      await Task.Delay(5000); // 5 seconds                           │
│  }                                                                  │
│                                                                     │
└─────────────────────────┬───────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────────┐
│  Kafka Cluster                                                      │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Topic: catalog-events (3 partitions)                               │
│  ┌──────────────┬──────────────┬──────────────┐                     │
│  │ Partition 0  │ Partition 1  │ Partition 2  │                     │
│  ├──────────────┼──────────────┼──────────────┤                     │
│  │ ProductId: 1 │ ProductId: 2 │ ProductId: 3 │                     │
│  │ ProductId: 4 │ ProductId: 5 │ ProductId: 6 │                     │
│  │ ...          │ ...          │ ...          │                     │
│  └──────────────┴──────────────┴──────────────┘                     │
│                                                                     │
│  Partitioning Strategy: Hash(AggregateId) % NumPartitions           │
│  Replication Factor: 3                                              │
│  Min In-Sync Replicas: 2                                            │
│  Retention: 7 days                                                  │
│                                                                     │
└─────────────────────────┬───────────────────────────────────────────┘
                          │
                          │ Consumer subscribes
                          │
                          ▼
╔═══════════════════════════════════════════════════════════════════════╗
║                        CONSUMER SIDE (Subscriber)                     ║
╚═══════════════════════════════════════════════════════════════════════╝

┌─────────────────────────────────────────────────────────────────────┐
│  KafkaEventConsumer (Background Service)                            │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Consumer Group: analytics-consumers                                │
│  Topics: catalog-events, order-events, payment-events               │
│  Auto-Offset-Commit: false (manual commit)                          │
│                                                                     │
│  protected override async Task ExecuteAsync(...)                    │
│  {                                                                  │
│      _consumer.Subscribe(topics);                                   │
│                                                                     │
│      while (!stoppingToken.IsCancellationRequested)                 │
│      {                                                              │
│          ┌──────────────────────────────────┐                       │
│          │ 1. Poll for Messages             │                       │
│          │    var result = _consumer        │                       │
│          │        .Consume(timeout: 1s);    │                       │
│          └──────────┬───────────────────────┘                       │
│                     │                                               │
│                     ▼                                               │
│          ┌──────────────────────────────────┐                       │
│          │ 2. Deserialize Event             │                       │
│          │    var eventType =               │                       │
│          │      result.Headers["event-type"]│                       │
│          │    var @event = Deserialize      │                       │
│          │      (result.Value, eventType);  │                       │
│          └──────────┬───────────────────────┘                       │
│                     │                                               │
│                     ▼                                               │
│          ┌──────────────────────────────────┐                       │
│          │ 3. Idempotency Check             │                       │
│          │    if (AlreadyProcessed(         │                       │
│          │        result.Offset,            │                       │
│          │        result.Partition))        │                       │
│          │    {                             │                       │
│          │        Skip & Commit;            │                       │
│          │    }                             │                       │
│          └──────────┬───────────────────────┘                       │
│                     │                                               │
│                     ▼                                               │
│          ┌──────────────────────────────────┐                       │
│          │ 4. Process Event                 │                       │
│          │    using var scope =             │                       │
│          │      _serviceProvider            │                       │
│          │        .CreateScope();           │                       │
│          │                                  │                       │
│          │    var handler = scope           │                       │
│          │      .GetRequiredService         │                       │
│          │      <IEventHandler<TEvent>>();  │                       │
│          │                                  │                       │
│          │    await handler.HandleAsync(    │                       │
│          │      @event,                     │                       │
│          │      cancellationToken);         │                       │
│          └──────────┬───────────────────────┘                       │
│                     │                                               │
│                     ▼                                               │
│          ┌──────────────────────────────────┐                       │
│          │ 5. Commit Offset (Manual)        │                       │
│          │    _consumer.Commit(result);     │                       │
│          │    // Only after successful      │                       │
│          │    // processing                 │                       │
│          └──────────────────────────────────┘                       │
│                                                                     │
│      } // while loop                                                │
│  }                                                                  │
│                                                                     │
│  Error Handling:                                                    │
│    • Transient errors → Retry (3 attempts)                          │
│    • Permanent errors → Send to DLQ (Dead Letter Queue)             │
│    • Poison messages → Skip & log                                   │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘

╔═══════════════════════════════════════════════════════════════════════╗
║                    CONSUMER GROUP COORDINATION                        ║
╚═══════════════════════════════════════════════════════════════════════╝

  Consumer Group: analytics-consumers (3 instances)
  
  ┌─────────────────────────────────────────────────────────┐
  │  Instance 1          Instance 2          Instance 3     │
  │  (Pod 1)             (Pod 2)             (Pod 3)        │
  ├─────────────────────────────────────────────────────────┤
  │  Partition 0         Partition 1         Partition 2    │
  │  Offset: 1000        Offset: 1050        Offset: 980    │
  │                                                         │
  │  Each consumer processes messages from assigned         │
  │  partition independently. Kafka ensures load balancing. │
  │                                                         │
  │  If Instance 2 dies:                                    │
  │    → Partition 1 reassigned to Instance 1 or 3          │
  │    → Rebalancing triggered automatically                │
  └─────────────────────────────────────────────────────────┘

╔═══════════════════════════════════════════════════════════════════════╗
║                         GUARANTEES                                    ║
╚═══════════════════════════════════════════════════════════════════════╝

  At-Least-Once Delivery:
    ✓ Outbox pattern ensures events not lost
    ✓ Manual commit after processing
    ✓ Retry on transient failures

  Ordering per Aggregate:
    ✓ Same AggregateId → Same Partition
    ✓ Partition = FIFO queue
    ✓ ProductId 123 events processed in order

  Idempotency:
    ✓ Consumer tracks processed offsets
    ✓ Handlers check for duplicate processing
    ✓ Use correlation-id for deduplication
```

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

### Order Creation Saga State Machine

```
┌─────────────────────────────────────────────────────────────────────────┐
│                   ORDER CREATION SAGA ORCHESTRATOR                      │
│                     (Distributed Transaction)                           │
└─────────────────────────────────────────────────────────────────────────┘

                           [SAGA START]
                                │
                                ▼
                        ┌───────────────┐
                        │   Created     │ ◄── Initial State
                        └───────┬───────┘
                                │
                    ┌───────────┴───────────┐
                    │  OrderCreated Event   │
                    └───────────┬───────────┘
                                │
                                ▼
╔════════════════════════════════════════════════════════════════╗
║  STEP 1: Reserve Inventory                                     ║
╚════════════════════════════════════════════════════════════════╝
                                │
                ┌───────────────┼───────────────┐
                │               │               │
                ▼               ▼               ▼
         ┌──────────┐   ┌─────────────┐  ┌──────────────┐
         │ Publish  │   │   Await     │  │  Timeout:    │
         │ Reserve  │──→│  Response   │  │  30 seconds  │
         │Inventory │   └─────┬───────┘  └──────┬───────┘
         └──────────┘         │                 │
                              │                 │ (Timeout)
                    ┌─────────┴─────────┐       │
                    │                   │       │
              [SUCCESS]            [FAILURE]    │
                    │                   │       │
                    ▼                   ▼       ▼
        ┌────────────────────┐   ┌────────────────────┐
        │InventoryReserved   │   │InventoryFailed     │
        │     State          │   │    State           │
        └─────────┬──────────┘   └─────────┬──────────┘
                  │                        │
                  │                        └──────┐
                  ▼                               │
╔═════════════════════════════════════════════════│═══════════════╗
║  STEP 2: Process Payment                        │               ║
╚═════════════════════════════════════════════════│═══════════════╝
                  │                               │
      ┌───────────┼───────────┐                   │
      │           │           │                   │
      ▼           ▼           ▼                   │
┌──────────┐ ┌─────────┐ ┌─────────┐              │
│ Publish  │ │  Await  │ │ Timeout │              │
│ Process  │→│Response │ │30 secs  │              │
│ Payment  │ └────┬────┘ └────┬────┘              │
└──────────┘      │           │                   │
                  │           │ (Timeout)         │
        ┌─────────┴─────┐     │                   │
        │               │     │                   │
   [SUCCESS]      [FAILURE]   │                   │
        │               │     │                   │
        ▼               ▼     ▼                   │
┌──────────────┐  ┌──────────────────┐            │
│PaymentProc.  │  │ PaymentFailed    │            │
│   State      │  │    State         │            │
└──────┬───────┘  └────────┬─────────┘            │
       │                   │                      │
       │                   └─────────┐            │
       ▼                             │            │
╔═════════════════════════════════════│═══════════│══════════════╗
║  STEP 3: Create Shipment            │           │              ║
╚═════════════════════════════════════│═══════════│══════════════╝
       │                              │           │
       ▼                              │           │
┌──────────────┐                      │           │
│ Publish      │                      │           │
│ Create       │                      │           │
│ Shipment     │                      │           │
└──────┬───────┘                      │           │
       │                              │           │
       │ [SUCCESS]                    │           │
       ▼                              │           │
┌──────────────────┐                  │           │
│ ShipmentCreated  │                  │           │
│     State        │                  │           │
└──────┬───────────┘                  │           │
       │                              │           │
       ▼                              │           │
╔═════════════════════════════════════│═══════════│══════════════╗
║  SAGA COMPLETION                    │           │              ║
╚═════════════════════════════════════│═══════════│══════════════╝
       │                              │           │
       ▼                              │           │
┌───────────────┐                     │           │
│  Completed    │ ◄── Final State     │           │
│  (Success)    │                     │           │
└───────┬───────┘                     │           │
        │                             │           │
        │ Publish: OrderConfirmed     │           │
        ▼                             │           │
   [END SUCCESS]                      │           │
                                      │           │
                                      ▼           ▼
╔═════════════════════════════════════════════════════════════╗
║              COMPENSATION FLOW (Rollback)                   ║
╚═════════════════════════════════════════════════════════════╝
                                      │
                          ┌───────────┴────────────┐
                          │   Compensating         │
                          │      State             │
                          └───────────┬────────────┘
                                      │
                  ┌───────────────────┼──────────────────┐
                  │                   │                  │
                  ▼                   ▼                  ▼
    ┌─────────────────────┐  ┌────────────────┐  ┌─────────────┐
    │ Compensation Step 3 │  │Compensation    │  │Compensation │
    │ (If shipment made)  │  │   Step 2       │  │   Step 1    │
    │ Cancel Shipment     │  │ Refund Payment │  │ Release     │
    │                     │  │                │  │ Inventory   │
    └─────────────────────┘  └────────────────┘  └─────────────┘
                  │                   │                  │
                  └───────────────────┼──────────────────┘
                                      │
                                      ▼
                          ┌────────────────────┐
                          │   Compensated      │
                          │   State (Failed)   │
                          └─────────┬──────────┘
                                    │
                                    │ Publish: OrderCancelled
                                    ▼
                               [END FAILURE]

╔═════════════════════════════════════════════════════════════════╗
║                    SAGA STATE TRANSITIONS                       ║
╚═════════════════════════════════════════════════════════════════╝

Status Enum:
  • Created           → Initial state
  • ReservingInventory→ Step 1 in progress
  • InventoryReserved → Step 1 complete
  • ProcessingPayment → Step 2 in progress
  • PaymentProcessed  → Step 2 complete
  • CreatingShipment  → Step 3 in progress
  • ShipmentCreated   → Step 3 complete
  • Completed         → Saga success
  • Compensating      → Rolling back
  • Compensated       → Rollback complete (failure)
  • Failed            → Unrecoverable error

Stored in saga_state table:
  {
    "id": "uuid",
    "correlation_id": "uuid",
    "order_id": "uuid",
    "current_step": "ProcessingPayment",
    "status": "ProcessingPayment",
    "state_data": {
      "inventory_reservation_id": "uuid",
      "payment_transaction_id": "string",
      "items": [...],
      "total_amount": 199.99
    },
    "created_at": "2024-10-15T10:00:00Z",
    "updated_at": "2024-10-15T10:00:15Z"
  }

Timeout Handling:
  • Each step has 30-second timeout
  • After timeout → Trigger compensation
  • SagaRecoveryService checks for stale sagas every 1 minute
  • Stale saga (> 2 minutes in same state) → Auto-compensate
```

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

### Resilience Patterns with Polly

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    RESILIENCE PATTERNS (Polly 8.5.0)                    │
│                  Protecting Against Transient Failures                  │
└─────────────────────────────────────────────────────────────────────────┘

╔═══════════════════════════════════════════════════════════════════════╗
║                    1. RETRY POLICY (Exponential Backoff)              ║
╚═══════════════════════════════════════════════════════════════════════╝

  HTTP Request to External Service
        │
        ▼
  ┌──────────────────────┐
  │  Attempt 1 (0ms)     │ ──→ [500 Server Error] ✗
  └──────────────────────┘
        │
        │ Wait: 2^1 = 2 seconds
        ▼
  ┌──────────────────────┐
  │  Attempt 2 (2s)      │ ──→ [503 Unavailable] ✗
  └──────────────────────┘
        │
        │ Wait: 2^2 = 4 seconds
        ▼
  ┌──────────────────────┐
  │  Attempt 3 (6s)      │ ──→ [200 OK] ✓
  └──────────────────────┘
        │
        ▼
   Return Success

Configuration:
  • Max Retries: 3
  • Backoff: Exponential (2^n seconds)
  • Jitter: +/- 20% randomization
  • Retryable Errors: 5xx, 408, 429
  • Non-Retryable: 4xx (except 408, 429)

Code:
  HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(
      retryCount: 3,
      sleepDurationProvider: retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
      onRetry: (outcome, timespan, retryCount, context) =>
      {
        Log.Warning("Retry {RetryCount} after {Delay}ms", 
          retryCount, timespan.TotalMilliseconds);
      }
    );

╔═══════════════════════════════════════════════════════════════════════╗
║                    2. CIRCUIT BREAKER PATTERN                         ║
╚═══════════════════════════════════════════════════════════════════════╝

                        ┌──────────────┐
                    ┌──→│   CLOSED     │◄──┐
                    │   │ (Normal)     │   │
                    │   └──────┬───────┘   │
                    │          │           │
                    │    Success < 50%     │
                    │    (5 consecutive    │
                    │     failures)        │
  Reset after 30s   │          │           │ Success
  no failures       │          ▼           │ rate > 50%
                    │   ┌──────────────┐   │
                    │   │     OPEN     │   │
                    │   │ (Failing)    │   │
                    │   │ Fast-fail all│   │
                    │   │  requests    │   │
                    │   └──────┬───────┘   │
                    │          │           │
                    │    After 30s         │
                    │    (Break Duration)  │
                    │          │           │
                    │          ▼           │
                    │   ┌──────────────┐   │
                    └───│  HALF-OPEN   │───┘
                        │ (Testing)    │
                        │ Allow 1 test │
                        │  request     │
                        └──────────────┘

State Transitions:
  CLOSED → OPEN:
    • 5 consecutive failures
    • Failure rate > 50% (in 10s window)
  
  OPEN → HALF-OPEN:
    • After 30 seconds (break duration)
    • Allow test request
  
  HALF-OPEN → CLOSED:
    • Test request succeeds
    • Resume normal operation
  
  HALF-OPEN → OPEN:
    • Test request fails
    • Reset break duration timer

Configuration:
  HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(
      handledEventsAllowedBeforeBreaking: 5,
      durationOfBreak: TimeSpan.FromSeconds(30),
      onBreak: (outcome, duration) =>
      {
        Log.Error("Circuit breaker opened for {Duration}s", 
          duration.TotalSeconds);
        // Send alert, update metrics
      },
      onReset: () =>
      {
        Log.Information("Circuit breaker reset");
      },
      onHalfOpen: () =>
      {
        Log.Warning("Circuit breaker half-open, testing");
      }
    );

╔═══════════════════════════════════════════════════════════════════════╗
║                    3. TIMEOUT POLICY                                  ║
╚═══════════════════════════════════════════════════════════════════════╝

  HTTP Request starts
        │
        ▼
  ┌──────────────────────────────────────┐
  │  Timeout: 30 seconds                 │
  ├──────────────────────────────────────┤
  │                                      │
  │  ┌────────┐                          │
  │  │  0s    │ Request sent             │
  │  └────────┘                          │
  │                                      │
  │  ┌────────┐                          │
  │  │ 10s    │ Waiting for response...  │
  │  └────────┘                          │
  │                                      │
  │  ┌────────┐                          │
  │  │ 20s    │ Still waiting...         │
  │  └────────┘                          │
  │                                      │
  │  ┌────────┐                          │
  │  │ 30s    │ TIMEOUT!                 │
  │  └────────┘                          │
  │                                      │
  │  Cancel request                      │
  │  Throw TimeoutRejectedException      │
  │                                      │
  └──────────────────────────────────────┘
        │
        ▼
  Log timeout & trigger fallback

Configuration:
  Policy.TimeoutAsync<HttpResponseMessage>(
    TimeSpan.FromSeconds(30)
  );

╔═══════════════════════════════════════════════════════════════════════╗
║                    4. FALLBACK POLICY                                 ║
╚═══════════════════════════════════════════════════════════════════════╝

  Primary Request
        │
        ▼
  ┌──────────────────────┐
  │ Call External API    │
  └──────┬───────────────┘
         │
    [FAILURE]
         │
         ▼
  ┌──────────────────────────────────────┐
  │  Fallback Strategy                   │
  ├──────────────────────────────────────┤
  │  1. Return cached response (if any)  │
  │  2. Return default value             │
  │  3. Return degraded response         │
  │  4. Forward to backup service        │
  └──────┬───────────────────────────────┘
         │
         ▼
  Return Fallback Result

Example: Product Catalog Service
  Primary: Get from Database
  Fallback: Return from Redis cache (stale data OK)
  
  Policy
    .HandleResult<ProductDto>(r => r == null)
    .Or<Exception>()
    .FallbackAsync(
      fallbackValue: await GetFromCache(),
      onFallbackAsync: async (outcome, context) =>
      {
        Log.Warning("Using fallback for {Operation}", 
          context.OperationKey);
      }
    );

╔═══════════════════════════════════════════════════════════════════════╗
║                    5. BULKHEAD ISOLATION                              ║
╚═══════════════════════════════════════════════════════════════════════╝

  Limits concurrent executions to prevent resource exhaustion

  ┌──────────────────────────────────────────────────────────┐
  │  Bulkhead: Max 10 concurrent requests                    │
  ├──────────────────────────────────────────────────────────┤
  │                                                          │
  │  ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐                      │
  │  │ 1  │ │ 2  │ │ 3  │ │ 4  │ │ 5  │  Active: 5           │
  │  └────┘ └────┘ └────┘ └────┘ └────┘                      │
  │                                                          │
  │  ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐                      │
  │  │ 6  │ │ 7  │ │ 8  │ │ 9  │ │ 10 │  Active: 10 (MAX)    │
  │  └────┘ └────┘ └────┘ └────┘ └────┘                      │
  │                                                          │
  │  ┌────┐ ┌────┐                                           │
  │  │ 11 │ │ 12 │  ← Queued (max queue: 5)                  │
  │  └────┘ └────┘                                           │
  │                                                          │
  │  Request 13 → ✗ REJECTED (queue full)                    │
  │                                                          │
  └──────────────────────────────────────────────────────────┘

Configuration:
  Policy.BulkheadAsync<HttpResponseMessage>(
    maxParallelization: 10,
    maxQueuingActions: 5,
    onBulkheadRejectedAsync: async context =>
    {
      Log.Warning("Bulkhead rejected request");
    }
  );

╔═══════════════════════════════════════════════════════════════════════╗
║                    COMBINING POLICIES (Pipeline)                      ║
╚═══════════════════════════════════════════════════════════════════════╝

  Request Flow through Policy Pipeline:

  HTTP Request
        │
        ▼
  ┌──────────────────────┐
  │ 1. Timeout (30s)     │  Outermost - Time bound
  └──────────┬───────────┘
             ▼
  ┌──────────────────────┐
  │ 2. Circuit Breaker   │  Fail-fast if broken
  └──────────┬───────────┘
             ▼
  ┌──────────────────────┐
  │ 3. Retry (3x)        │  Retry transient failures
  └──────────┬───────────┘
             ▼
  ┌──────────────────────┐
  │ 4. Bulkhead (10)     │  Limit concurrency
  └──────────┬───────────┘
             ▼
  ┌──────────────────────┐
  │ 5. Actual HTTP Call  │  Make the request
  └──────────────────────┘

Code:
  services.AddHttpClient<ICatalogService, CatalogService>()
    .AddPolicyHandler(GetTimeoutPolicy())          // Outer
    .AddPolicyHandler(GetCircuitBreakerPolicy())
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetBulkheadPolicy());        // Inner

Order matters! Timeout wraps Circuit Breaker wraps Retry.
```

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

### OpenTelemetry Observability Stack

```
┌─────────────────────────────────────────────────────────────────────────┐
│                   OBSERVABILITY ARCHITECTURE                            │
│        (OpenTelemetry + Jaeger + Prometheus + Grafana + Loki)           │
└─────────────────────────────────────────────────────────────────────────┘

╔═══════════════════════════════════════════════════════════════════════╗
║                    APPLICATION INSTRUMENTATION                        ║
╚═══════════════════════════════════════════════════════════════════════╝

┌─────────────────────────────────────────────────────────────────────┐
│  Microservice (e.g., Catalog.API)                                   │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌────────────────────────────────────────────────────┐             │
│  │  OpenTelemetry SDK                                 │             │
│  ├────────────────────────────────────────────────────┤             │
│  │                                                    │             │
│  │  Instrumentation Libraries:                        │             │
│  │  ✓ ASP.NET Core (HTTP requests)                    │             │
│  │  ✓ HttpClient (outgoing requests)                  │             │
│  │  ✓ Entity Framework Core (DB queries)              │             │
│  │  ✓ Kafka Producer/Consumer                         │             │
│  │  ✓ Redis (caching operations)                      │             │
│  │                                                    │             │
│  │  Custom Instrumentation:                           │             │
│  │  • ActivitySource: "Catalog.API"                   │             │
│  │  • Meter: "Catalog.API"                            │             │
│  │  • Custom spans for business logic                 │             │
│  │  • Custom metrics (orders_created, etc.)           │             │
│  │                                                    │             │
│  └────────────┬──────────┬──────────┬─────────────────┘             │
│               │          │          │                               │
│         [TRACES]    [METRICS]  [LOGS]                               │
│               │          │          │                               │
└───────────────┼──────────┼──────────┼───────────────────────────────┘
                │          │          │
                │          │          │
                ▼          ▼          ▼
┌─────────────────────────────────────────────────────────────────────┐
│  OpenTelemetry Collector (Optional - Advanced Scenarios)            │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Receivers:                                                         │
│    • OTLP (gRPC/HTTP) ← From services                               │
│    • Jaeger ← Legacy format                                         │
│    • Prometheus ← Scraping                                          │
│                                                                     │
│  Processors:                                                        │
│    • Batch: Aggregate before export                                 │
│    • Filter: Remove sensitive data                                  │
│    • Attributes: Add resource attributes                            │
│    • Sampling: Reduce volume (tail sampling)                        │
│                                                                     │
│  Exporters:                                                         │
│    • Jaeger ← Traces                                                │
│    • Prometheus ← Metrics                                           │
│    • Loki ← Logs                                                    │
│                                                                     │
└──────────────┬──────────┬──────────┬───────────────────────────────┘
               │          │          │
               ▼          ▼          ▼
   ┌───────────────┐ ┌──────────┐ ┌──────────────┐
   │    Jaeger     │ │Prometheus│ │     Loki     │
   │  (Tracing)    │ │(Metrics) │ │   (Logs)     │
   └───────┬───────┘ └─────┬────┘ └──────┬───────┘
           │               │              │
           └───────────────┼──────────────┘
                           │
                           ▼
                   ┌───────────────┐
                   │    Grafana    │
                   │(Visualization)│
                   └───────────────┘

╔═══════════════════════════════════════════════════════════════════════╗
║                         DISTRIBUTED TRACING                           ║
╚═══════════════════════════════════════════════════════════════════════╝

  HTTP Request with Correlation-Id
        │
        ▼
  ┌─────────────────────────────────────────────────────────────┐
  │  Trace: order-creation-20241015-abc123                      │
  │  TraceId: 5b8aa5a2d2c872e8321cf37308d69df2                  │
  ├─────────────────────────────────────────────────────────────┤
  │                                                             │
  │  ┌─────────────────────────────────────────────────────┐    │
  │  │ Span 1: API Gateway                                 │    │
  │  │ SpanId: 1234567890abcdef                            │    │
  │  │ Duration: 450ms                                     │    │
  │  │ ├─ Attributes:                                      │    │
  │  │ │  • http.method: POST                              │    │
  │  │ │  • http.route: /api/orders                        │    │
  │  │ │  • http.status_code: 201                          │    │
  │  │ └─ Tags: component=api-gateway                      │    │
  │  └──────────┬──────────────────────────────────────────┘    │
  │             │                                               │
  │             ▼                                               │
  │  ┌─────────────────────────────────────────────────────┐    │
  │  │ Span 2: Order Service - CreateOrderCommand          │    │
  │  │ ParentSpanId: 1234567890abcdef                      │    │
  │  │ SpanId: 2345678901bcdefg                            │    │
  │  │ Duration: 420ms                                     │    │
  │  │ ├─ Attributes:                                      │    │
  │  │ │  • service.name: order-service                    │    │
  │  │ │  • command: CreateOrderCommand                    │    │
  │  │ │  • order.id: ord-123                              │    │
  │  │ └─ Events:                                          │    │
  │  │    • ValidationSucceeded (10ms)                     │    │
  │  │    • SagaStarted (15ms)                             │    │
  │  └──────────┬──────────────────────────────────────────┘    │
  │             │                                               │
  │             ├─────────────────┬─────────────────┐           │
  │             ▼                 ▼                 ▼           │
  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
  │  │ Span 3:      │  │ Span 4:      │  │ Span 5:      │       │
  │  │ Inventory    │  │ Payment      │  │ Shipping     │       │
  │  │ Service      │  │ Service      │  │ Service      │       │
  │  │ Duration:    │  │ Duration:    │  │ Duration:    │       │
  │  │ 150ms        │  │ 180ms        │  │ 120ms        │       │
  │  └──────────────┘  └──────────────┘  └──────────────┘       │
  │                                                             │
  │  Each span includes:                                        │
  │    • Start/End timestamps                                   │
  │    • Duration                                               │
  │    • Parent relationship                                    │
  │    • Attributes (metadata)                                  │
  │    • Events (point-in-time occurrences)                     │
  │    • Status (OK, ERROR)                                     │
  │                                                             │
  └─────────────────────────────────────────────────────────────┘

Trace visualization in Jaeger UI:
  
  API Gateway          ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓ 450ms
    Order Service        ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓ 420ms
      Inventory            ▓▓▓▓▓▓▓▓ 150ms
      Payment                     ▓▓▓▓▓▓▓▓▓ 180ms
      Shipping                            ▓▓▓▓▓▓ 120ms
  
  Timeline: 0ms ────────────────────────────────────> 450ms

╔═══════════════════════════════════════════════════════════════════════╗
║                         METRICS COLLECTION                            ║
╚═══════════════════════════════════════════════════════════════════════╝

┌─────────────────────────────────────────────────────────────────────┐
│  Metrics Types                                                      │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  1. Counter (Cumulative)                                            │
│     ┌────────────────────────────────────┐                          │
│     │ orders_created_total               │                          │
│     │ Value: 1,523 (always increasing)   │                          │
│     │ Labels: {status=success}           │                          │
│     └────────────────────────────────────┘                          │
│                                                                     │
│  2. Histogram (Distribution)                                        │
│     ┌────────────────────────────────────┐                          │
│     │ http_request_duration_seconds      │                          │
│     │ Buckets:                           │                          │
│     │   0.1s: 1000 requests              │                          │
│     │   0.5s: 1800 requests              │                          │
│     │   1.0s: 1950 requests              │                          │
│     │   5.0s: 2000 requests              │                          │
│     │ P50: 0.3s, P95: 1.2s, P99: 3.5s    │                          │
│     └────────────────────────────────────┘                          │
│                                                                     │
│  3. Gauge (Current Value)                                           │
│     ┌────────────────────────────────────┐                          │
│     │ active_connections                 │                          │
│     │ Value: 42 (can go up/down)         │                          │
│     └────────────────────────────────────┘                          │
│                                                                     │
│  4. UpDownCounter                                                   │
│     ┌────────────────────────────────────┐                          │
│     │ inventory_quantity                 │                          │
│     │ Value: 500 (increases/decreases)   │                          │
│     │ ProductId: 123                     │                          │
│     └────────────────────────────────────┘                          │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘

Custom Metrics Example:

  public class OrderMetrics
  {
      private readonly Counter<long> _ordersCreated;
      private readonly Histogram<double> _orderValue;
      private readonly Gauge<int> _pendingOrders;

      public OrderMetrics(IMeterFactory factory)
      {
          var meter = factory.Create("Order.API");
          
          _ordersCreated = meter.CreateCounter<long>(
              "orders.created",
              unit: "orders",
              description: "Total orders created"
          );
          
          _orderValue = meter.CreateHistogram<double>(
              "orders.value",
              unit: "USD",
              description: "Order value distribution"
          );
          
          _pendingOrders = meter.CreateGauge<int>(
              "orders.pending",
              unit: "orders",
              description: "Pending orders count"
          );
      }

      public void RecordOrder(decimal amount)
      {
          _ordersCreated.Add(1, 
              new KeyValuePair<string, object>("status", "success")
          );
          _orderValue.Record((double)amount);
      }
  }

Prometheus Scraping:
  • Each service exposes /metrics endpoint
  • Prometheus scrapes every 15 seconds
  • Metrics stored in time-series database
  • Retention: 30 days

╔═══════════════════════════════════════════════════════════════════════╗
║                         STRUCTURED LOGGING                            ║
╚═══════════════════════════════════════════════════════════════════════╝

┌─────────────────────────────────────────────────────────────────────┐
│  Serilog Structured Logging                                         │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Log.Information(                                                   │
│      "Order {OrderId} created for user {UserId} " +                 │
│      "with {ItemCount} items totaling {Total:C}",                   │
│      order.Id,                                                      │
│      order.UserId,                                                  │
│      order.Items.Count,                                             │
│      order.TotalAmount                                              │
│  );                                                                 │
│                                                                     │
│  JSON Output:                                                       │
│  {                                                                  │
│    "@timestamp": "2024-10-15T10:30:45.123Z",                        │
│    "level": "Information",                                          │
│    "messageTemplate": "Order {OrderId} created...",                 │
│    "message": "Order ord-123 created for user usr-456...",          │
│    "properties": {                                                  │
│      "OrderId": "ord-123",                                          │
│      "UserId": "usr-456",                                           │
│      "ItemCount": 3,                                                │
│      "Total": 199.99,                                               │
│      "CorrelationId": "5b8aa5a2d2c872e8321cf37308d69df2",           │
│      "TraceId": "5b8aa5a2d2c872e8321cf37308d69df2",                 │
│      "SpanId": "2345678901bcdefg",                                  │
│      "ServiceName": "order-service",                                │
│      "Environment": "Production",                                   │
│      "MachineName": "order-pod-abc123"                              │
│    }                                                                │
│  }                                                                  │
│                                                                     │
│  Sinks:                                                             │
│    • Console (Development)                                          │
│    • Loki (Production) ← Grafana Loki for aggregation               │
│    • Elasticsearch (Production) ← Full-text search                  │
│                                                                     │
│  Log Levels:                                                        │
│    • Verbose: Detailed diagnostic                                   │
│    • Debug: Internal system events                                  │
│    • Information: General flow                                      │
│    • Warning: Unexpected but handled                                │
│    • Error: Failures, exceptions                                    │
│    • Fatal: Critical failures                                       │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘

╔═══════════════════════════════════════════════════════════════════════╗
║                    CORRELATION & CONTEXT PROPAGATION                  ║
╚═══════════════════════════════════════════════════════════════════════╝

  Request Flow with Correlation:

  Client Request
      │
      ├─ Generate: Correlation-Id = uuid
      │
      ▼
  ┌──────────────────────────┐
  │ API Gateway              │
  │ Add to HTTP Headers:     │
  │   X-Correlation-Id: uuid │
  │   traceparent: W3C format│
  └──────────┬───────────────┘
             │
             ├─ Propagate headers
             ▼
  ┌──────────────────────────┐
  │ Order Service            │
  │ Extract from headers     │
  │ Set Activity.TraceId     │
  │ Enrich logs with ID      │
  └──────────┬───────────────┘
             │
             ├─ Publish to Kafka
             │  with correlation ID
             ▼
  ┌──────────────────────────┐
  │ Kafka Message            │
  │ Headers:                 │
  │   correlation-id: uuid   │
  │   trace-id: xxx          │
  └──────────┬───────────────┘
             │
             ├─ Consumer reads
             ▼
  ┌──────────────────────────┐
  │ Inventory Service        │
  │ Extract correlation-id   │
  │ Continue trace           │
  └──────────────────────────┘

  All logs, traces, metrics linked by Correlation-Id!
  → Click on trace in Jaeger
  → See all related logs in Loki
  → See related metrics in Prometheus

╔═══════════════════════════════════════════════════════════════════════╗
║                         GRAFANA DASHBOARDS                            ║
╚═══════════════════════════════════════════════════════════════════════╝

  Pre-provisioned Dashboards:
    • Service Overview (RED metrics: Rate, Errors, Duration)
    • Order Processing Pipeline
    • Database Performance
    • Kafka Topic Metrics
    • Infrastructure (CPU, Memory, Disk)
    • Business Metrics (Sales, Revenue, Conversions)

  Example: Order Service Dashboard
    ┌────────────────────────────────────────┐
    │ Requests/sec   │  Error Rate   │ P95   │
    │      250       │     0.5%      │ 450ms │
    ├────────────────────────────────────────┤
    │ [Request Rate Graph]                   │
    │      ▁▂▃▄▅▆▇█▇▆▅▄▃▂▁                   │
    ├────────────────────────────────────────┤
    │ [Latency Histogram]                    │
    │ P50: 200ms | P95: 450ms | P99: 800ms   │
    ├────────────────────────────────────────┤
    │ [Error Rate by Endpoint]               │
    │ /api/orders: 0.2%                      │
    │ /api/orders/{id}: 0.1%                 │
    └────────────────────────────────────────┘

  Alerting:
    • Error rate > 5% → PagerDuty
    • P95 latency > 1s → Slack
    • Circuit breaker open → Email
    • Saga compensation rate > 10% → Alert
```

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

### API Gateway Request Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    API GATEWAY (YARP 2.1.0) REQUEST FLOW                │
│               Reverse Proxy + Auth + Rate Limiting + Routing            │
└─────────────────────────────────────────────────────────────────────────┘

╔═══════════════════════════════════════════════════════════════════════╗
║                         INCOMING REQUEST                              ║
╚═══════════════════════════════════════════════════════════════════════╝

  Client HTTP Request
  POST https://api.example.com/api/orders
  Headers:
    Authorization: Bearer eyJhbGc...
    Content-Type: application/json
    Idempotency-Key: req-12345
        │
        ▼
┌─────────────────────────────────────────────────────────────────────┐
│  MIDDLEWARE PIPELINE (Order matters!)                               │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌────────────────────────────────────────────────────┐             │
│  │ 1. HTTPS Redirection                               │             │
│  │    • Enforce HTTPS                                 │             │
│  │    • Redirect HTTP → HTTPS (301)                   │             │
│  └────────────────────────┬───────────────────────────┘             │
│                           │                                         │
│  ┌────────────────────────▼───────────────────────────┐             │
│  │ 2. CORS Middleware                                 │             │
│  │    • Check Origin header                           │             │
│  │    • Allow: https://app.example.com                │             │
│  │    • Methods: GET, POST, PUT, DELETE               │             │
│  │    • Headers: Authorization, Content-Type          │             │
│  │    • Credentials: true                             │             │
│  └────────────────────────┬───────────────────────────┘             │
│                           │                                         │
│  ┌────────────────────────▼───────────────────────────┐             │
│  │ 3. Correlation ID Middleware                       │             │
│  │    • Extract or generate X-Correlation-Id          │             │
│  │    • Set Activity.TraceId                          │             │
│  │    • Enrich logs with correlation ID               │             │
│  │    CorrelationId: 5b8aa5a2-d2c8-72e8-321c-...      │             │
│  └────────────────────────┬───────────────────────────┘             │
│                           │                                         │
│  ┌────────────────────────▼───────────────────────────┐             │
│  │ 4. Request Logging Middleware                      │             │
│  │    • Log request method, path, headers             │             │
│  │    • Start timer for duration tracking             │             │
│  │    Log: "HTTP POST /api/orders started"            │             │
│  └────────────────────────┬───────────────────────────┘             │
│                           │                                         │
│  ┌────────────────────────▼───────────────────────────┐             │
│  │ 5. Rate Limiting Middleware (.NET 9)               │             │
│  │    ┌──────────────────────────────────┐            │             │
│  │    │ Policy: Fixed Window             │            │             │
│  │    │ • Window: 1 minute               │            │             │
│  │    │ • Limit: 100 requests            │            │             │
│  │    │ • Current: 45/100                │            │             │
│  │    │ • Client IP: 203.0.113.45        │            │             │
│  │    └──────────────────────────────────┘            │             │
│  │    ✓ ALLOWED (45 < 100)                            │             │
│  │                                                    │             │
│  │    If EXCEEDED:                                    │             │
│  │      → Return 429 Too Many Requests                │             │
│  │      → Headers:                                    │             │
│  │         X-RateLimit-Limit: 100                     │             │
│  │         X-RateLimit-Remaining: 0                   │             │
│  │         X-RateLimit-Reset: 1697456789              │             │
│  │         Retry-After: 42                            │             │
│  └────────────────────────┬───────────────────────────┘             │
│                           │                                         │
│  ┌────────────────────────▼───────────────────────────┐             │
│  │ 6. JWT Authentication Middleware                   │             │
│  │    ┌──────────────────────────────────┐            │             │
│  │    │ Extract Bearer Token             │            │             │
│  │    │ Token: eyJhbGciOiJIUzI1NiIs...   │            │             │
│  │    └──────────┬───────────────────────┘            │             │
│  │               │                                    │             │
│  │    ┌──────────▼───────────────────────┐            │             │
│  │    │ Validate JWT                     │            │             │
│  │    │ • Signature valid? ✓             │            │             │
│  │    │ • Not expired? ✓                 │            │             │
│  │    │ • Issuer correct? ✓              │            │             │
│  │    │ • Audience correct? ✓            │            │             │
│  │    └──────────┬───────────────────────┘            │             │
│  │               │                                    │             │
│  │    ┌──────────▼───────────────────────┐            │             │
│  │    │ Extract Claims                   │            │             │
│  │    │ • UserId: usr-123                │            │             │
│  │    │ • Role: Customer                 │            │             │
│  │    │ • Email: user@example.com        │            │             │
│  │    │ • Expiry: 2024-10-15T12:00:00Z   │            │             │
│  │    └──────────┬───────────────────────┘            │             │
│  │               │                                    │             │
│  │    ✓ AUTHENTICATED                                 │             │
│  │    Set HttpContext.User (ClaimsPrincipal)          │             │
│  │                                                    │             │
│  │    If INVALID:                                     │             │
│  │      → Return 401 Unauthorized                     │             │
│  │      → WWW-Authenticate: Bearer error=...          │             │
│  └────────────────────────┬───────────────────────────┘             │
│                           │                                         │
│  ┌────────────────────────▼───────────────────────────┐             │
│  │ 7. Authorization Middleware (Optional)             │             │
│  │    • Check if user has required role               │             │
│  │    • [Authorize(Roles = "Admin")] attribute        │             │
│  │    • Policy-based authorization                    │             │
│  │    ✓ AUTHORIZED (Role: Customer allowed)           │             │
│  │                                                    │             │
│  │    If FORBIDDEN:                                   │             │
│  │      → Return 403 Forbidden                        │             │
│  └────────────────────────┬───────────────────────────┘             │
│                           │                                         │
└───────────────────────────┼─────────────────────────────────────────┘
                            │
                            ▼
╔═══════════════════════════════════════════════════════════════════════╗
║                        YARP REVERSE PROXY                             ║
╚═══════════════════════════════════════════════════════════════════════╝
                            │
            ┌───────────────┼───────────────┐
            │               │               │
            ▼               ▼               ▼
  ┌─────────────────┐ ┌─────────────┐ ┌─────────────┐
  │ Route Matching  │ │ Transform   │ │Load Balance │
  └────────┬────────┘ └──────┬──────┘ └──────┬──────┘
           │                 │               │
           ▼                 ▼               ▼
  ┌────────────────────────────────────────────────────┐
  │ Route: order-route                                 │
  │ Match:                                             │
  │   Path: /api/orders/{**catch-all}                  │
  │   Methods: [POST, GET, PUT, DELETE]                │
  │                                                    │
  │ Transform:                                         │
  │   From: /api/orders/{**catch-all}                  │
  │   To:   /api/{**catch-all}                         │
  │   PathPattern: /api/{**catch-all}                  │
  │                                                    │
  │   Add Headers:                                     │
  │     X-Forwarded-For: client-ip                     │
  │     X-Original-Path: /api/orders                   │
  │     X-Correlation-Id: 5b8aa5a2-d2c8...             │
  │                                                    │
  │ Cluster: order-cluster                             │
  │   Destinations:                                    │
  │     • order-pod-1: http://10.0.0.15:8080           │
  │     • order-pod-2: http://10.0.0.16:8080           │
  │                                                    │
  │   LoadBalancing: RoundRobin                        │
  │   Selected: order-pod-2 (10.0.0.16:8080)           │
  │                                                    │
  │   HealthCheck:                                     │
  │     • Active: true                                 │
  │     • Interval: 30s                                │
  │     • Path: /health                                │
  │     • Status: ✓ Healthy (both pods)                │
  │                                                    │
  │   CircuitBreaker:                                  │
  │     • State: CLOSED (healthy)                      │
  │                                                    │
  │   Timeout:                                         │
  │     • Request: 30s                                 │
  │                                                    │
  └────────────────────┬───────────────────────────────┘
                       │
                       ▼
╔═══════════════════════════════════════════════════════════════════════╗
║                      PROXY TO BACKEND SERVICE                         ║
╚═══════════════════════════════════════════════════════════════════════╝
                       │
        HTTP Request to Order Service
        POST http://10.0.0.16:8080/api
        Headers:
          Authorization: Bearer eyJhbGc...
          Content-Type: application/json
          X-Correlation-Id: 5b8aa5a2...
          X-Forwarded-For: 203.0.113.45
          X-Original-Path: /api/orders
                       │
                       ▼
        ┌──────────────────────────┐
        │  Order Service           │
        │  (Pod: order-pod-2)      │
        │  IP: 10.0.0.16:8080      │
        └──────────┬───────────────┘
                   │
                   │ Process request
                   │ Create order
                   │ Save to database
                   │ Publish events
                   │
                   ▼
        HTTP Response 201 Created
        {
          "orderId": "ord-789",
          "status": "pending",
          "totalAmount": 199.99
        }
                   │
                   ▼
╔═══════════════════════════════════════════════════════════════════════╗
║                       RESPONSE PIPELINE                               ║
╚═══════════════════════════════════════════════════════════════════════╝
                   │
        ┌──────────▼───────────┐
        │ YARP Proxy           │
        │ • Forward response   │
        │ • Add headers        │
        └──────────┬───────────┘
                   │
        ┌──────────▼───────────┐
        │ Response Logging     │
        │ • Log status: 201    │
        │ • Log duration: 420ms│
        └──────────┬───────────┘
                   │
        ┌──────────▼───────────┐
        │ Add Response Headers │
        │ X-Correlation-Id     │
        │ X-Request-Duration   │
        └──────────┬───────────┘
                   │
                   ▼
        Return to Client
        HTTP/1.1 201 Created
        Content-Type: application/json
        X-Correlation-Id: 5b8aa5a2...
        X-Request-Duration: 420ms
        
        {
          "orderId": "ord-789",
          "status": "pending",
          "totalAmount": 199.99
        }

╔═══════════════════════════════════════════════════════════════════════╗
║                     HEALTH CHECK AGGREGATION                          ║
╚═══════════════════════════════════════════════════════════════════════╝

  GET /health
        │
        ▼
  ┌────────────────────────────────────────┐
  │ Aggregate Health Checks                │
  ├────────────────────────────────────────┤
  │ ✓ Self (API Gateway): Healthy          │
  │ ✓ Identity Service: Healthy            │
  │ ✓ Catalog Service: Healthy             │
  │ ✓ Order Service: Healthy               │
  │ ✓ Payment Service: Degraded            │
  │ ✗ Inventory Service: Unhealthy         │
  │                                        │
  │ Overall Status: Degraded               │
  └────────────────────────────────────────┘

  Response:
  {
    "status": "Degraded",
    "checks": {
      "self": "Healthy",
      "identity-service": "Healthy",
      "catalog-service": "Healthy",
      "order-service": "Healthy",
      "payment-service": "Degraded",
      "inventory-service": "Unhealthy"
    },
    "timestamp": "2024-10-15T10:30:00Z"
  }
```

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

### Test Architecture & Pyramid

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         TEST PYRAMID                                    │
│                    (Bottom-Heavy Distribution)                          │
└─────────────────────────────────────────────────────────────────────────┘

                           ▲ Slower
                           │ More Expensive
                           │ Brittle
                           │
                      ┌────────────┐
                      │    E2E     │  ← 10% (Slow, Expensive)
                      │   Tests    │    • Full system integration
                      │    ~50     │    • Real infrastructure
                      └────────────┘    • UI + API + DB + Kafka
                     /              \
                    /                \
               ┌──────────────────────┐
               │  Integration Tests   │  ← 30% (Medium Speed)
               │        ~300          │    • Service integration
               │  (API + DB + Cache)  │    • Testcontainers
               └──────────────────────┘    • Real dependencies
              /                        \
             /                          \
        ┌────────────────────────────────┐
        │       Unit Tests               │  ← 60% (Fast, Cheap)
        │          ~1000                 │    • Business logic
        │  (Pure functions, handlers)    │    • Mocked dependencies
        └────────────────────────────────┘    • No I/O
           │
           │ Faster
           │ Cheaper
           │ Reliable
           ▼

╔═══════════════════════════════════════════════════════════════════════╗
║                          1. UNIT TESTS                                ║
╚═══════════════════════════════════════════════════════════════════════╝

  Target: Business Logic, Handlers, Validators, Domain Models
  Framework: xUnit 2.9.2
  Mocking: Moq 4.20.70
  Assertions: FluentAssertions 7.0.0
  Data: AutoFixture 4.18.1, Bogus 35.6.1

┌─────────────────────────────────────────────────────────────────────┐
│  Test Structure (AAA Pattern)                                       │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  public class CreateProductCommandHandlerTests                      │
│  {                                                                  │
│      [Fact]                                                         │
│      public async Task Handle_ValidCommand_CreatesProduct()         │
│      {                                                              │
│          // ─────────────────────────────────────────               │
│          // ARRANGE                                                 │
│          // ─────────────────────────────────────────               │
│          var fixture = new Fixture();                               │
│          var command = fixture.Build<CreateProductCommand>()        │
│              .With(x => x.Price, 99.99m)                            │
│              .Create();                                             │
│                                                                     │
│          var dbContext = new Mock<ICatalogDbContext>();             │
│          var eventBus = new Mock<IEventBus>();                      │
│                                                                     │
│          var handler = new CreateProductCommandHandler(             │
│              dbContext.Object,                                      │
│              eventBus.Object                                        │
│          );                                                         │
│                                                                     │
│          // ─────────────────────────────────────────               │
│          // ACT                                                     │
│          // ─────────────────────────────────────────               │
│          var result = await handler.Handle(                         │
│              command,                                               │
│              CancellationToken.None                                 │
│          );                                                         │
│                                                                     │
│          // ─────────────────────────────────────────               │
│          // ASSERT                                                  │
│          // ─────────────────────────────────────────               │
│          result.Should().NotBeNull();                               │
│          result.IsSuccess.Should().BeTrue();                        │
│          result.Value.Name.Should().Be(command.Name);               │
│                                                                     │
│          dbContext.Verify(                                          │
│              x => x.SaveChangesAsync(                               │
│                  It.IsAny<CancellationToken>()),                    │
│              Times.Once                                             │
│          );                                                         │
│                                                                     │
│          eventBus.Verify(                                           │
│              x => x.PublishAsync(                                   │
│                  It.IsAny<ProductCreatedEvent>(),                   │
│                  It.IsAny<CancellationToken>()),                    │
│              Times.Once                                             │
│          );                                                         │
│      }                                                              │
│                                                                     │
│      [Theory]                                                       │
│      [InlineData("")]                                               │
│      [InlineData(null)]                                             │
│      public async Task Handle_InvalidName_ThrowsException(          │
│          string invalidName)                                        │
│      {                                                              │
│          // Test validation logic                                   │
│      }                                                              │
│  }                                                                  │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘

  Characteristics:
    ✓ Fast execution (< 100ms each)
    ✓ No external dependencies
    ✓ Deterministic (same input = same output)
    ✓ Isolated (no shared state)
    ✓ Run in parallel
    ✓ ~1000 tests run in < 10 seconds

╔═══════════════════════════════════════════════════════════════════════╗
║                      2. INTEGRATION TESTS                             ║
╚═══════════════════════════════════════════════════════════════════════╝

  Target: API endpoints, Repository, Database, Cache, Message Bus
  Framework: xUnit + WebApplicationFactory + Testcontainers
  Containers: PostgreSQL, Redis, Kafka

┌─────────────────────────────────────────────────────────────────────┐
│  Test with Real Infrastructure (Testcontainers)                     │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  public class CatalogApiTests : IAsyncLifetime                      │
│  {                                                                  │
│      private PostgreSqlContainer _dbContainer;                      │
│      private RedisContainer _redisContainer;                        │
│      private WebApplicationFactory<Program> _factory;               │
│      private HttpClient _client;                                    │
│                                                                     │
│      // ───────────────────────────────────────                     │
│      // TEST SETUP (Runs once per test class)                       │
│      // ───────────────────────────────────────                     │
│      public async Task InitializeAsync()                            │
│      {                                                              │
│          // Start PostgreSQL container                              │
│          _dbContainer = new PostgreSqlBuilder()                     │
│              .WithImage("postgres:16-alpine")                       │
│              .WithDatabase("catalog_test")                          │
│              .WithUsername("test")                                  │
│              .WithPassword("test")                                  │
│              .Build();                                              │
│          await _dbContainer.StartAsync();                           │
│                                                                     │
│          // Start Redis container                                   │
│          _redisContainer = new RedisBuilder()                       │
│              .WithImage("redis:7-alpine")                           │
│              .Build();                                              │
│          await _redisContainer.StartAsync();                        │
│                                                                     │
│          // Create test server with real containers                 │
│          _factory = new WebApplicationFactory<Program>()            │
│              .WithWebHostBuilder(builder =>                         │
│              {                                                      │
│                  builder.ConfigureServices(services =>              │
│                  {                                                  │
│                      // Replace with test containers                │
│                      services.AddDbContext<CatalogDbContext>(       │
│                          options => options.UseNpgsql(              │
│                              _dbContainer                           │
│                                  .GetConnectionString())            │
│                      );                                             │
│                                                                     │
│                      services.AddStackExchangeRedisCache(           │
│                          options =>                                 │
│                          {                                          │
│                              options.Configuration =                │
│                                  _redisContainer                    │
│                                      .GetConnectionString();        │
│                          }                                          │
│                      );                                             │
│                                                                     │
│                      // Mock Kafka (too heavy for tests)            │
│                      services.AddSingleton<IEventBus,               │
│                          FakeEventBus>();                           │
│                  });                                                │
│              });                                                    │
│                                                                     │
│          _client = _factory.CreateClient();                         │
│                                                                     │
│          // Run migrations                                          │
│          using var scope = _factory.Services.CreateScope();         │
│          var dbContext = scope.ServiceProvider                      │
│              .GetRequiredService<CatalogDbContext>();               │
│          await dbContext.Database.MigrateAsync();                   │
│      }                                                              │
│                                                                     │
│      // ───────────────────────────────────────                     │
│      // TEST EXECUTION                                              │
│      // ───────────────────────────────────────                     │
│      [Fact]                                                         │
│      public async Task CreateProduct_ValidData_ReturnsCreated()     │
│      {                                                              │
│          // Arrange                                                 │
│          var command = new                                          │
│          {                                                          │
│              Name = "Test Product",                                 │
│              Description = "Test Description",                      │
│              Price = 99.99m,                                        │
│              CategoryId = 1                                         │
│          };                                                         │
│                                                                     │
│          // Act                                                     │
│          var response = await _client.PostAsJsonAsync(              │
│              "/api/products",                                       │
│              command                                                │
│          );                                                         │
│                                                                     │
│          // Assert                                                  │
│          response.StatusCode.Should().Be(                           │
│              HttpStatusCode.Created                                 │
│          );                                                         │
│                                                                     │
│          var product = await response.Content                       │
│              .ReadFromJsonAsync<ProductDto>();                      │
│          product.Should().NotBeNull();                              │
│          product!.Name.Should().Be("Test Product");                 │
│          product.Price.Should().Be(99.99m);                         │
│                                                                     │
│          // Verify in database                                      │
│          using var scope = _factory.Services.CreateScope();         │
│          var dbContext = scope.ServiceProvider                      │
│              .GetRequiredService<CatalogDbContext>();               │
│          var saved = await dbContext.Products                       │
│              .FirstOrDefaultAsync(p => p.Id == product.Id);         │
│          saved.Should().NotBeNull();                                │
│      }                                                              │
│                                                                     │
│      // ───────────────────────────────────────                     │
│      // CLEANUP                                                     │
│      // ───────────────────────────────────────                     │
│      public async Task DisposeAsync()                               │
│      {                                                              │
│          await _dbContainer.DisposeAsync();                         │
│          await _redisContainer.DisposeAsync();                      │
│          await _factory.DisposeAsync();                             │
│      }                                                              │
│  }                                                                  │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘

  Testcontainers Architecture:

    Test Runner (xUnit)
         │
         ├─→ Start PostgreSQL Container (Docker)
         │   • Ephemeral (deleted after test)
         │   • Isolated per test class
         │   • Real PostgreSQL 16
         │
         ├─→ Start Redis Container (Docker)
         │   • Ephemeral
         │   • Real Redis 7
         │
         └─→ Start Application (WebApplicationFactory)
             • In-memory HTTP server
             • Uses real containers
             • Full middleware pipeline

  Characteristics:
    ✓ Real database interactions
    ✓ Real cache behavior
    ✓ Isolated per test class
    ✓ Slower (1-5 seconds each)
    ✓ High confidence
    ✓ ~300 tests run in 2-3 minutes

╔═══════════════════════════════════════════════════════════════════════╗
║                      3. END-TO-END TESTS                              ║
╚═══════════════════════════════════════════════════════════════════════╝

  Target: Complete user workflows across multiple services
  Tools: Docker Compose + HTTP Client + Database assertions

┌─────────────────────────────────────────────────────────────────────┐
│  Full System Test: Create Order Workflow                            │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  [Setup]                                                            │
│    • docker-compose up -d (all services + infrastructure)           │
│    • Wait for health checks                                         │
│    • Seed test data                                                 │
│                                                                     │
│  [Test Flow]                                                        │
│    1. Register User → Identity Service                              │
│       POST /api/identity/register                                   │
│       → 201 Created                                                 │
│                                                                     │
│    2. Login → Identity Service                                      │
│       POST /api/identity/login                                      │
│       → 200 OK + JWT token                                          │
│                                                                     │
│    3. Create Product → Catalog Service                              │
│       POST /api/catalog/products                                    │
│       → 201 Created                                                 │
│                                                                     │
│    4. Add Inventory → Inventory Service                             │
│       POST /api/inventory                                           │
│       → 200 OK                                                      │
│                                                                     │
│    5. Create Order → Order Service (Saga starts!)                   │
│       POST /api/orders                                              │
│       → 201 Created                                                 │
│       → OrderId: ord-123                                            │
│                                                                     │
│    6. Wait for Saga Completion (async)                              │
│       Poll: GET /api/orders/ord-123                                 │
│       Wait until status = "Confirmed"                               │
│       Timeout: 30 seconds                                           │
│                                                                     │
│    7. Verify Inventory Reserved                                     │
│       GET /api/inventory/products/prod-456                          │
│       Assert: availableQuantity decreased                           │
│                                                                     │
│    8. Verify Payment Processed                                      │
│       GET /api/payments?orderId=ord-123                             │
│       Assert: status = "Processed"                                  │
│                                                                     │
│    9. Verify Shipment Created                                       │
│       GET /api/shipping?orderId=ord-123                             │
│       Assert: trackingNumber exists                                 │
│                                                                     │
│   10. Verify Notification Sent                                      │
│       Check email mock/stub                                         │
│       Assert: "Order Confirmed" email sent                          │
│                                                                     │
│  [Teardown]                                                         │
│    • docker-compose down -v                                         │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘

  Characteristics:
    ✓ Full system integration
    ✓ Real infrastructure
    ✓ Kafka events flowing
    ✓ Saga orchestration
    ✓ Very slow (30+ seconds each)
    ✓ Brittle (many failure points)
    ✓ Run in CI/CD pipeline only
    ✓ ~50 tests run in 30+ minutes

╔═══════════════════════════════════════════════════════════════════════╗
║                         TEST DATA GENERATION                          ║
╚═══════════════════════════════════════════════════════════════════════╝

  AutoFixture (Quick random data):
    var fixture = new Fixture();
    var product = fixture.Create<Product>();
    // Generates random but valid data

  Bogus (Realistic fake data):
    var faker = new Faker<Product>()
        .RuleFor(p => p.Name, f => f.Commerce.ProductName())
        .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
        .RuleFor(p => p.Price, f => f.Random.Decimal(1, 1000))
        .RuleFor(p => p.Sku, f => f.Commerce.Ean13());
    
    var products = faker.Generate(100);
    // Generates realistic product data

╔═══════════════════════════════════════════════════════════════════════╗
║                         CI/CD PIPELINE                                ║
╚═══════════════════════════════════════════════════════════════════════╝

  GitHub Actions Workflow:

    ┌─────────────────────────────────────────┐
    │ 1. Build & Compile                      │
    │    dotnet build                         │
    │    Duration: 2 minutes                  │
    └─────────────────┬───────────────────────┘
                      │
                      ▼
    ┌─────────────────────────────────────────┐
    │ 2. Unit Tests (Parallel)                │
    │    dotnet test --filter Category=Unit  │
    │    ~1000 tests                          │
    │    Duration: 30 seconds                 │
    └─────────────────┬───────────────────────┘
                      │
                      ▼
    ┌─────────────────────────────────────────┐
    │ 3. Integration Tests (Sequential)       │
    │    dotnet test --filter Category=Integ │
    │    ~300 tests                           │
    │    Duration: 3 minutes                  │
    └─────────────────┬───────────────────────┘
                      │
                      ▼
    ┌─────────────────────────────────────────┐
    │ 4. E2E Tests (On main branch only)      │
    │    docker-compose -f e2e.yml up         │
    │    ~50 tests                            │
    │    Duration: 30 minutes                 │
    └─────────────────┬───────────────────────┘
                      │
                      ▼
    ┌─────────────────────────────────────────┐
    │ 5. Code Coverage Report                 │
    │    Target: > 80% coverage               │
    │    Publish to Codecov                   │
    └─────────────────────────────────────────┘

  Total Pipeline: ~35 minutes (with E2E), ~5 minutes (without)
```

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
