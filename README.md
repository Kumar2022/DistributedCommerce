# DistributedCommerce

A modern, cloud-native e-commerce platform built with microservices architecture, designed for scalability, resilience, and high performance. This project demonstrates enterprise-grade patterns and practices for building distributed systems using .NET and Kubernetes.

## 🚀 Overview

DistributedCommerce is a fully-featured e-commerce platform that breaks down traditional monolithic architecture into discrete, independently deployable microservices. Each service is responsible for a specific business capability, enabling teams to develop, deploy, and scale features independently.

### Why Microservices?

This architecture was chosen to provide:

- **Scalability** - Scale individual services based on demand rather than the entire application
- **Resilience** - Fault isolation ensures one failing service doesn't bring down the entire system
- **Technology Flexibility** - Choose the best technology stack for each service's specific needs
- **Team Autonomy** - Teams can work independently on different services without stepping on each other's toes
- **Faster Deployment** - Deploy updates to individual services without redeploying everything

## 🏗️ Architecture

The platform is built following Domain-Driven Design (DDD) principles and Clean Architecture patterns. Each microservice is structured with clear boundaries and responsibilities.

### Services

The platform consists of 8 core microservices:

#### 1. **Identity Service**
Handles user authentication and authorization using JWT tokens. Manages user accounts, roles, and permissions across the platform.

#### 2. **Catalog Service**
Manages product catalog, including product information, categories, pricing, and product search functionality. This is the heart of product discovery.

#### 3. **Order Service**
Orchestrates the order lifecycle from creation to completion. Implements the Saga pattern for distributed transactions across multiple services.

#### 4. **Payment Service**
Processes payments securely, handles payment gateway integration, and manages transaction records. Implements idempotency to prevent duplicate charges.

#### 5. **Inventory Service**
Tracks stock levels, manages warehouse inventory, and handles stock reservations. Ensures products can't be oversold through real-time inventory management.

#### 6. **Shipping Service**
Manages shipping logistics, calculates shipping costs, tracks deliveries, and integrates with shipping providers.

#### 7. **Notification Service**
Sends transactional notifications via email, SMS, and push notifications. Handles order confirmations, shipping updates, and promotional messages.

#### 8. **Analytics Service**
Collects and processes business metrics, generates reports, and provides insights into customer behavior and business performance.

### Building Blocks

The platform leverages shared building blocks to ensure consistency across services:

- **Domain** - Common domain models, value objects, and domain events
- **Application** - CQRS, MediatR handlers, and application services
- **Infrastructure** - Database access, caching, and external integrations
- **EventBus** - Asynchronous messaging using RabbitMQ for inter-service communication
- **Authentication** - Shared JWT authentication and authorization logic
- **Observability** - Distributed tracing, metrics, and logging with OpenTelemetry
- **Resilience** - Circuit breakers, retries, and fallback policies using Polly
- **Saga** - Distributed transaction orchestration for complex workflows
- **Idempotency** - Ensures operations can be safely retried without side effects

## 🛠️ Technology Stack

### Core Technologies
- **.NET 8** - Latest long-term support version for high-performance APIs
- **ASP.NET Core** - Web framework for building RESTful APIs
- **Entity Framework Core** - ORM for database access
- **MediatR** - In-process messaging for CQRS implementation
- **FluentValidation** - Robust input validation
- **AutoMapper** - Object-to-object mapping

### Infrastructure & DevOps
- **Docker** - Containerization for consistent deployments
- **Kubernetes** - Container orchestration for production deployments
- **Helm** - Package management for Kubernetes
- **Terraform** - Infrastructure as Code (IaC)

### Data & Messaging
- **PostgreSQL** - Primary relational database
- **MongoDB** - Document database for analytics and flexible schemas
- **Redis** - Distributed caching and session storage
- **RabbitMQ** - Message broker for asynchronous communication

### Observability
- **OpenTelemetry** - Distributed tracing and metrics collection
- **Prometheus** - Metrics storage and alerting
- **Grafana** - Metrics visualization and dashboards
- **Jaeger** - Distributed tracing visualization
- **Seq** - Structured logging and log aggregation

### Quality & Testing
- **xUnit** - Unit and integration testing framework
- **FluentAssertions** - Expressive test assertions
- **Moq** - Mocking framework for unit tests
- **Testcontainers** - Integration testing with real dependencies
- **K6** - Load testing and performance benchmarking

## 📋 Prerequisites

Before you begin, ensure you have the following installed:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (version 8.0 or later)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (version 20.10 or later)
- [kubectl](https://kubernetes.io/docs/tasks/tools/) (version 1.25 or later)
- [Helm](https://helm.sh/docs/intro/install/) (version 3.10 or later) - Optional, for Kubernetes deployments
- [Git](https://git-scm.com/downloads) - For version control
- A code editor (Visual Studio 2022, Rider, or VS Code recommended)

### Optional Tools
- [k9s](https://k9scli.io/) - Kubernetes CLI management tool
- [Postman](https://www.postman.com/) or [Insomnia](https://insomnia.rest/) - API testing
- [pgAdmin](https://www.pgadmin.org/) - PostgreSQL administration

## 🚦 Getting Started

### Quick Start with Docker Compose

The fastest way to get the entire platform running locally:

```bash
# Clone the repository
git clone https://github.com/yourusername/DistributedCommerce.git
cd DistributedCommerce

# Start all services with Docker Compose
docker-compose up -d

# Check service health
docker-compose ps
```

The services will be available at:
- Identity API: http://localhost:5001
- Catalog API: http://localhost:5002
- Order API: http://localhost:5003
- Payment API: http://localhost:5004
- Inventory API: http://localhost:5005
- Shipping API: http://localhost:5006
- Notification API: http://localhost:5007
- Analytics API: http://localhost:5008

### Running Services Individually

If you prefer to run services separately:

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run a specific service (example: Catalog API)
cd src/Services/Catalog/Catalog.API
dotnet run
```

### Database Setup

Each service manages its own database. Migrations are applied automatically on startup in development mode.

To manually apply migrations:

```bash
# Example for Catalog Service
cd src/Services/Catalog/Catalog.Infrastructure
dotnet ef database update
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run unit tests only
dotnet test --filter Category=Unit

# Run integration tests
dotnet test --filter Category=Integration

# Run with code coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportFormat=opencover
```

Or use the provided script:

```bash
./run-all-tests.sh
```

## 🐳 Docker Deployment

### Building Images

Build all service images:

```bash
# Build all images
docker-compose build

# Build a specific service
docker build -f src/Services/Catalog/Catalog.API/Dockerfile -t catalog-api .
```

Or use the Kubernetes build script:

```bash
cd deployment/kubernetes
./build-images.sh
```

### Docker Compose Configurations

- `docker-compose.yml` - Core services
- `docker-compose.observability.yml` - Monitoring stack (Prometheus, Grafana, Jaeger)
- `docker-compose.override.yml` - Development overrides

Start everything including observability:

```bash
docker-compose -f docker-compose.yml -f docker-compose.observability.yml up -d
```

## ☸️ Kubernetes Deployment

### Local Kubernetes (Development)

The project includes scripts for easy local Kubernetes deployment:

```bash
cd deployment/kubernetes

# Setup local environment (creates namespaces, secrets, etc.)
./setup-local.sh

# Deploy infrastructure (databases, message brokers)
kubectl apply -k infrastructure/

# Deploy all services
./deploy.sh

# Or deploy everything at once
./deploy-complete.sh
```

### Kubernetes Components

The deployment includes:

**Infrastructure**
- PostgreSQL clusters for each service
- MongoDB for analytics
- Redis for caching
- RabbitMQ for messaging

**Observability**
- Prometheus for metrics
- Grafana for visualization
- Jaeger for distributed tracing
- OpenTelemetry Collector

**Security**
- Network policies for service isolation
- Pod security policies
- Secret management with External Secrets Operator

**Reliability**
- Service mesh with Istio (optional)
- Circuit breakers and retry policies
- Pod disruption budgets
- Horizontal Pod Autoscaling (HPA)

### Accessing Services in Kubernetes

```bash
# Port forward to a service
kubectl port-forward svc/catalog-api 5002:80 -n distributed-commerce

# View service logs
kubectl logs -f deployment/catalog-api -n distributed-commerce

# Check service health
kubectl get pods -n distributed-commerce
```

### Monitoring and Observability

Access monitoring dashboards:

```bash
# Grafana
kubectl port-forward svc/grafana 3000:3000 -n monitoring

# Prometheus
kubectl port-forward svc/prometheus 9090:9090 -n monitoring

# Jaeger
kubectl port-forward svc/jaeger-query 16686:16686 -n monitoring
```

Default credentials:
- Grafana: admin/admin (change on first login)

## 📊 Monitoring & Observability

### Distributed Tracing

All services are instrumented with OpenTelemetry to provide end-to-end request tracing:

- View traces in Jaeger UI at http://localhost:16686
- Track request flow across services
- Identify performance bottlenecks
- Debug distributed transactions

### Metrics

Key metrics are collected and visualized in Grafana:

- Request rate, latency, and error rate (RED metrics)
- Resource utilization (CPU, memory, disk)
- Business metrics (orders placed, revenue, inventory levels)
- Database performance metrics

### Logging

Structured logging is implemented across all services:

- Logs are collected by the OpenTelemetry Collector
- View logs in Seq at http://localhost:5341
- Correlated with traces using correlation IDs
- Search and filter across all services

## 🔧 Configuration

### Environment Variables

Each service can be configured via environment variables or `appsettings.json`:

```bash
# Database connection
ConnectionStrings__DefaultConnection="Host=localhost;Database=catalog;Username=postgres;Password=postgres"

# RabbitMQ
EventBus__Connection="amqp://guest:guest@localhost:5672"

# Redis
Redis__Configuration="localhost:6379"

# OpenTelemetry
OpenTelemetry__ServiceName="catalog-api"
OpenTelemetry__OtlpEndpoint="http://localhost:4317"
```

### Service Configuration Files

Configuration is managed at multiple levels:

- `appsettings.json` - Default configuration
- `appsettings.Development.json` - Development overrides
- `appsettings.Production.json` - Production settings
- Environment variables - Runtime overrides
- Kubernetes ConfigMaps and Secrets - K8s deployments

## 🔐 Security

### Authentication & Authorization

- JWT-based authentication
- Role-based access control (RBAC)
- API key authentication for service-to-service communication
- OAuth 2.0 / OpenID Connect support

### Security Best Practices

- All secrets stored in Kubernetes secrets or Azure Key Vault
- TLS/SSL for all service communication in production
- Input validation on all endpoints
- SQL injection prevention via parameterized queries
- XSS protection with proper encoding
- CORS configuration for web clients
- Rate limiting to prevent abuse

## 🧪 Testing Strategy

### Unit Tests

- Test domain logic in isolation
- Mock external dependencies
- Fast execution (< 1 second per test suite)
- Located in `tests/Unit/`

### Integration Tests

- Test service integration with real dependencies
- Use Testcontainers for database and message broker
- Test API endpoints end-to-end
- Located in `tests/Integration/`

### Load Tests

- Performance and stress testing with K6
- Test scalability under load
- Identify performance bottlenecks
- Located in `tests/Load/`

### Running Tests by Category

```bash
# Unit tests
dotnet test tests/Unit/

# Integration tests (requires Docker)
dotnet test tests/Integration/

# Load tests
cd tests/Load/LoadTests
k6 run load-test.js
```

## 📈 Performance Optimization

### Caching Strategy

- **Distributed Caching**: Redis for shared cache across service instances
- **Response Caching**: HTTP caching for GET requests
- **In-Memory Caching**: Hot data cached in application memory
- **Cache Invalidation**: Event-driven cache updates

### Database Optimization

- **Indexes**: Optimized indexes on frequently queried columns
- **Query Optimization**: EF Core query performance tuning
- **Connection Pooling**: Efficient database connection management
- **Read Replicas**: Separate read/write databases for scalability

### API Optimization

- **Pagination**: All list endpoints support pagination
- **Compression**: Response compression enabled (gzip/brotli)
- **Async/Await**: Non-blocking I/O operations throughout
- **Minimal APIs**: Reduced overhead for high-throughput endpoints

## 🔄 CI/CD Pipeline

### GitHub Actions Workflows

The project includes automated workflows for:

- **Build & Test**: Runs on every push and PR
- **Docker Image Build**: Builds and pushes images to registry
- **Kubernetes Deployment**: Automated deployment to clusters
- **Security Scanning**: Vulnerability scanning with Trivy
- **Code Quality**: Static analysis and linting

### Deployment Pipeline

```
Code Push → Build → Unit Tests → Integration Tests → Security Scan → Build Images → Deploy to Dev → E2E Tests → Deploy to Staging → Deploy to Production
```

## 🗺️ Roadmap

### Current Features ✅
- Core microservices architecture
- Event-driven communication
- Distributed tracing and monitoring
- Kubernetes deployment
- Automated testing suite

### Upcoming Features 🚧
- [ ] GraphQL API gateway
- [ ] Event sourcing for Order service
- [ ] Machine learning recommendations
- [ ] Multi-tenant support
- [ ] Mobile API with BFF pattern
- [ ] Advanced fraud detection
- [ ] Real-time inventory sync
- [ ] Serverless functions for notifications

## 🤝 Contributing

We welcome contributions! Here's how you can help:

1. **Fork the repository**
2. **Create a feature branch** (`git checkout -b feature/amazing-feature`)
3. **Commit your changes** (`git commit -m 'Add some amazing feature'`)
4. **Push to the branch** (`git push origin feature/amazing-feature`)
5. **Open a Pull Request**

### Coding Standards

- Follow C# coding conventions
- Write unit tests for new features
- Update documentation as needed
- Keep commits atomic and well-described
- Ensure all tests pass before submitting PR

### Code Review Process

All submissions require review:
- At least one approval from maintainers
- All CI checks must pass
- Code coverage should not decrease
- Documentation must be updated

## 📝 Project Structure

```
DistributedCommerce/
├── src/
│   ├── BuildingBlocks/          # Shared libraries
│   │   ├── Application/         # CQRS, MediatR
│   │   ├── Domain/              # Domain models
│   │   ├── Infrastructure/      # Data access, caching
│   │   ├── EventBus/            # Message broker integration
│   │   ├── Authentication/      # JWT authentication
│   │   ├── Observability/       # Tracing, metrics
│   │   ├── Resilience/          # Circuit breakers, retries
│   │   ├── Saga/                # Distributed transactions
│   │   └── Idempotency/         # Idempotent operations
│   └── Services/
│       ├── Identity/            # Authentication service
│       ├── Catalog/             # Product catalog
│       ├── Order/               # Order management
│       ├── Payment/             # Payment processing
│       ├── Inventory/           # Stock management
│       ├── Shipping/            # Logistics
│       ├── Notification/        # Messaging
│       └── Analytics/           # Business intelligence
├── tests/
│   ├── Unit/                    # Unit tests
│   ├── Integration/             # Integration tests
│   ├── Load/                    # Performance tests
│   └── Shared/                  # Test utilities
├── deployment/
│   ├── docker/                  # Dockerfiles
│   ├── kubernetes/              # K8s manifests
│   ├── helm/                    # Helm charts
│   ├── terraform/               # IaC scripts
│   ├── grafana/                 # Dashboards
│   ├── prometheus/              # Monitoring config
│   └── otel-collector/          # Tracing config
├── docs/                        # Documentation
├── scripts/                     # Automation scripts
└── DistributedCommerce.sln      # Solution file
```

## 📚 Documentation

Additional documentation is available in the `/docs` folder:

- [Architecture Decision Records (ADRs)](docs/architecture/)
- [API Documentation](docs/api/)
- [Deployment Guides](docs/deployment/)
- [Developer Guides](docs/development/)
- [Troubleshooting](docs/troubleshooting/)

## 🐛 Troubleshooting

### Common Issues

**Services won't start**
```bash
# Check Docker is running
docker ps

# Check logs
docker-compose logs [service-name]
```

**Database connection errors**
```bash
# Verify database is running
docker-compose ps postgres

# Check connection string in appsettings.json
```

**Port conflicts**
```bash
# Check what's using the port
lsof -i :[port-number]

# Update port in docker-compose.yml if needed
```

**Kubernetes pods not starting**
```bash
# Check pod status
kubectl get pods -n distributed-commerce

# View pod logs
kubectl logs [pod-name] -n distributed-commerce

# Describe pod for events
kubectl describe pod [pod-name] -n distributed-commerce
```

### Getting Help

- 📖 Check the [documentation](docs/)
- 🐛 [Open an issue](https://github.com/yourusername/DistributedCommerce/issues)
- 💬 Join our [Discussions](https://github.com/yourusername/DistributedCommerce/discussions)
- 📧 Email: support@distributedcommerce.com


## 🙏 Acknowledgments

This project was built using patterns and practices from:

- Microsoft's [eShopOnContainers](https://github.com/dotnet-architecture/eShopOnContainers)
- Domain-Driven Design by Eric Evans
- Microservices Patterns by Chris Richardson
- Building Microservices by Sam Newman

Special thanks to the .NET community and all contributors who have helped shape this project.

## 🌟 Show Your Support

If you find this project helpful, please consider:

- ⭐ Starring the repository
- 🐛 Reporting bugs
- 💡 Suggesting new features
- 🤝 Contributing to the codebase
- 📢 Spreading the word

---

**Built with ❤️ by developers, for developers**

*Last updated: October 2024*
