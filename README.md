# DistributedCommerce

Enterprise-grade microservices e-commerce platform built with .NET 9, demonstrating production-ready patterns for distributed systems.

## Tech Stack

**Core**
- .NET 9.0
- ASP.NET Core 9.0
- Entity Framework Core 9.0.9
- C# 13 with nullable reference types

**API Gateway**
- YARP 2.1.0 (Reverse Proxy)
- Built-in .NET 9 rate limiting

**Data**
- PostgreSQL 16 (primary database)
- Redis 7 (caching)

**Messaging**
- Apache Kafka 7.5.0 (Confluent)
- Schema Registry (Avro schemas)

**Observability**
- OpenTelemetry 1.9.0+
- Jaeger (distributed tracing)
- Elasticsearch (logging)
- Serilog 8.0.3

**Patterns & Libraries**
- MediatR 12.4.1 (CQRS)
- Polly 8.5.0 (resilience)
- FluentValidation
- AutoMapper

**Infrastructure**
- Docker & Docker Compose
- Kubernetes
- Helm

**Testing**
- xUnit 2.9.2
- FluentAssertions 7.0.0
- Moq 4.20.70
- Testcontainers

## Architecture

9 microservices communicating via Kafka, accessed through YARP API Gateway:

1. **API Gateway** - YARP reverse proxy, authentication, rate limiting
2. **Identity** - Authentication, authorization, user management
3. **Catalog** - Product catalog, categories, search
4. **Order** - Order orchestration, Saga pattern
5. **Payment** - Payment processing, idempotency
6. **Inventory** - Stock management, reservations
7. **Shipping** - Logistics, tracking
8. **Notification** - Email, SMS, push notifications
9. **Analytics** - Metrics, reporting

**Building Blocks (shared libraries)**
- Domain, Application, Infrastructure
- EventBus (Kafka), Authentication, Observability
- Resilience, Saga, Idempotency

See [ARCHITECTURE.md](ARCHITECTURE.md) for system design and [LLD.md](LLD.md) for technical details.

## Prerequisites

- .NET 9 SDK
- Docker Desktop
- kubectl (for K8s deployment)

## Quick Start

### Local Development (Docker Compose)

```bash
# Start infrastructure
cd deployment/docker
docker-compose up -d

# Infrastructure services will be available at:
# PostgreSQL: localhost:5432
# Redis: localhost:6379
# Kafka: localhost:9092, localhost:9093
# Schema Registry: localhost:8081
# Jaeger UI: localhost:16686
# Elasticsearch: localhost:9200
```

### Build and Run Services

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run specific service
cd src/Services/Catalog/Catalog.API
dotnet run

# Or run API Gateway
cd src/ApiGateways/ApiGateway
dotnet run
```

### Service Ports

- API Gateway: 5000
- Identity: 5001
- Catalog: 5002
- Order: 5003
- Payment: 5004
- Inventory: 5005
- Shipping: 5006
- Notification: 5007
- Analytics: 5008

### Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Unit/Catalog.UnitTests

# Run with coverage
dotnet test /p:CollectCoverage=true

# Or use script
./run-all-tests.sh
```

## Kubernetes Deployment

```bash
cd deployment/kubernetes

# Setup local environment
./setup-local.sh

# Deploy infrastructure
kubectl apply -k infrastructure/

# Deploy all services
./deploy.sh

# Check status
kubectl get pods -n distributed-commerce

# Access API Gateway
kubectl port-forward svc/api-gateway 8080:80 -n distributed-commerce
```

### Monitoring

```bash
# Jaeger UI
kubectl port-forward svc/jaeger-query 16686:16686 -n monitoring

# View logs
kubectl logs -f deployment/catalog-api -n distributed-commerce
```

## Project Structure

```
DistributedCommerce/
├── src/
│   ├── ApiGateways/
│   │   └── ApiGateway/              # YARP-based gateway
│   ├── BuildingBlocks/              # Shared libraries
│   │   ├── Application/             # CQRS, MediatR
│   │   ├── Authentication/          # JWT
│   │   ├── Domain/                  # Domain models
│   │   ├── EventBus/                # Kafka integration
│   │   ├── Idempotency/             # Idempotent operations
│   │   ├── Infrastructure/          # Data access, caching
│   │   ├── Observability/           # OpenTelemetry
│   │   ├── Resilience/              # Polly policies
│   │   └── Saga/                    # Distributed transactions
│   └── Services/
│       ├── Analytics/               # Business intelligence
│       ├── Catalog/                 # Product management
│       ├── Identity/                # Auth service
│       ├── Inventory/               # Stock management
│       ├── Notification/            # Messaging
│       ├── Order/                   # Order orchestration
│       ├── Payment/                 # Payment processing
│       └── Shipping/                # Logistics
├── tests/
│   ├── Unit/                        # Unit tests
│   ├── Integration/                 # Integration tests
│   ├── Load/                        # Performance tests
│   └── Shared/                      # Test utilities
├── deployment/
│   ├── docker/                      # Docker Compose
│   ├── kubernetes/                  # K8s manifests
│   ├── helm/                        # Helm charts
│   └── terraform/                   # IaC
└── docs/                            # Documentation
```

## Key Patterns

- **Clean Architecture** - Domain at center, dependencies point inward
- **DDD** - Aggregates, entities, value objects, domain events
- **CQRS** - Separate read/write models with MediatR
- **Event Sourcing** - Domain events for audit and integration
- **Saga Pattern** - Distributed transaction orchestration
- **Outbox Pattern** - Reliable event publishing
- **Circuit Breaker** - Fault tolerance with Polly
- **Retry & Timeout** - Resilient HTTP calls
- **Idempotency** - Safe retry mechanisms

## Configuration

Services use layered configuration:

1. `appsettings.json` - Defaults
2. `appsettings.{Environment}.json` - Environment-specific
3. Environment variables - Runtime overrides
4. Kubernetes ConfigMaps/Secrets - K8s deployments

Example environment variables:

```bash
ConnectionStrings__DefaultConnection="Host=localhost;Database=catalog;Username=postgres;Password=postgres"
EventBus__BootstrapServers="localhost:9092"
Redis__Configuration="localhost:6379"
OpenTelemetry__JaegerEndpoint="http://localhost:4317"
```

## Development Guidelines

- Each service owns its database (database-per-service)
- Services communicate via Kafka events (async) or HTTP (sync)
- Use MediatR for in-process messaging
- Follow CQRS for complex operations
- Write unit tests for domain logic
- Write integration tests for APIs
- Use Testcontainers for integration tests

## Troubleshooting

**Services won't start**
```bash
# Check Docker
docker ps

# Check logs
docker-compose logs [service-name]
```

**Database connection errors**
```bash
# Verify PostgreSQL
docker-compose ps postgres

# Check connection string in appsettings.json
```

**Kafka connection issues**
```bash
# Verify Kafka
docker-compose logs kafka

# Check Zookeeper
docker-compose logs zookeeper
```

**Kubernetes pods failing**
```bash
# Check pod status
kubectl get pods -n distributed-commerce

# View pod logs
kubectl logs [pod-name] -n distributed-commerce

# Describe pod
kubectl describe pod [pod-name] -n distributed-commerce
```

## Contributing

1. Fork repository
2. Create feature branch
3. Write tests
4. Ensure all tests pass
5. Submit pull request
---
