# Architecture

## System Overview

DistributedCommerce is a microservices-based e-commerce platform using event-driven architecture. Services communicate asynchronously via Kafka and synchronously through HTTP, all accessed via YARP API Gateway.

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        Clients                               │
│              (Web, Mobile, Third-party)                      │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│                    API Gateway (YARP)                        │
│    • Reverse Proxy      • JWT Auth     • Rate Limiting      │
└────────────────────────────┬────────────────────────────────┘
                             │
        ┌────────────────────┴────────────────────┐
        ▼                                         ▼
┌──────────────────┐                    ┌──────────────────┐
│  Microservices   │                    │  Microservices   │
│                  │                    │                  │
│ • Identity       │                    │ • Inventory      │
│ • Catalog        │                    │ • Shipping       │
│ • Order          │                    │ • Notification   │
│ • Payment        │                    │ • Analytics      │
└────────┬─────────┘                    └─────────┬────────┘
         │                                        │
         └────────────────┬───────────────────────┘
                          ▼
              ┌─────────────────────┐
              │  Apache Kafka       │
              │  (Event Bus)        │
              └─────────────────────┘
                          │
        ┌─────────────────┴─────────────────┐
        ▼                                   ▼
┌──────────────────┐              ┌──────────────────┐
│   PostgreSQL     │              │      Redis       │
│  (Per Service)   │              │    (Caching)     │
└──────────────────┘              └──────────────────┘
```

## Services

### 1. API Gateway
**Technology**: YARP 2.1.0  
**Responsibilities**:
- Reverse proxy to backend services
- JWT authentication/authorization
- Rate limiting (.NET 9 built-in)
- Request routing and transformation
- CORS handling
- Health checks aggregation

**Endpoints**: Routes to all downstream services

---

### 2. Identity Service
**Database**: PostgreSQL  
**Responsibilities**:
- User registration and authentication
- JWT token generation and validation
- Role-based access control (RBAC)
- User profile management
- Password reset and email verification

**Key Events**:
- `UserRegistered`
- `UserLoggedIn`
- `PasswordChanged`

---

### 3. Catalog Service
**Database**: PostgreSQL  
**Responsibilities**:
- Product CRUD operations
- Category management
- Product search and filtering
- Price management
- Product images and metadata

**Key Events**:
- `ProductCreated`
- `ProductUpdated`
- `PriceChanged`
- `ProductDeleted`

---

### 4. Order Service
**Database**: PostgreSQL  
**Pattern**: Saga Orchestrator  
**Responsibilities**:
- Order creation and management
- Order status tracking
- Saga orchestration for distributed transactions
- Order history and queries

**Key Events**:
- `OrderCreated`
- `OrderConfirmed`
- `OrderCancelled`
- `OrderCompleted`

**Saga Steps**:
1. Reserve inventory → `InventoryReserved`
2. Process payment → `PaymentProcessed`
3. Create shipment → `ShipmentCreated`
4. Send notification → `NotificationSent`

---

### 5. Payment Service
**Database**: PostgreSQL  
**Pattern**: Idempotency  
**Responsibilities**:
- Payment processing
- Payment gateway integration
- Transaction management
- Refund processing
- Idempotency key validation

**Key Events**:
- `PaymentInitiated`
- `PaymentProcessed`
- `PaymentFailed`
- `RefundProcessed`

---

### 6. Inventory Service
**Database**: PostgreSQL  
**Responsibilities**:
- Stock level management
- Inventory reservation and release
- Warehouse management
- Stock alerts and notifications
- Prevent overselling

**Key Events**:
- `InventoryReserved`
- `InventoryReleased`
- `StockLevelChanged`
- `LowStockAlert`

---

### 7. Shipping Service
**Database**: PostgreSQL  
**Responsibilities**:
- Shipping cost calculation
- Carrier integration
- Tracking management
- Delivery status updates
- Address validation

**Key Events**:
- `ShipmentCreated`
- `ShipmentDispatched`
- `ShipmentDelivered`
- `TrackingUpdated`

---

### 8. Notification Service
**Database**: PostgreSQL (template storage)  
**Responsibilities**:
- Email notifications
- SMS notifications
- Push notifications
- Template management
- Delivery tracking

**Key Events**:
- `NotificationSent`
- `NotificationFailed`
- `EmailDelivered`

**Consumes**: Events from all services

---

### 9. Analytics Service
**Database**: PostgreSQL  
**Responsibilities**:
- Business metrics collection
- Report generation
- Customer behavior analysis
- Sales analytics
- Real-time dashboards

**Consumes**: Events from all services for analytics

---

## Communication Patterns

### Synchronous (HTTP)
- Client → API Gateway → Services
- Used for immediate responses (queries, commands needing instant feedback)
- Implemented with HttpClient + Polly resilience

### Asynchronous (Kafka)
- Service → Kafka → Service(s)
- Used for event notifications, long-running processes
- Ensures loose coupling and eventual consistency
- Implements Outbox pattern for reliability

## Data Architecture

### Database-per-Service
Each service has its own PostgreSQL database:
- `identity_db`
- `catalog_db`
- `order_db`
- `payment_db`
- `inventory_db`
- `shipping_db`
- `notification_db`
- `analytics_db`

### Caching Strategy
Redis used for:
- Distributed cache (product catalog, user sessions)
- Rate limiting counters
- Temporary data storage

### Event Store
Kafka topics for domain events:
- `identity-events`
- `catalog-events`
- `order-events`
- `payment-events`
- `inventory-events`
- `shipping-events`
- `notification-events`

## Cross-Cutting Concerns

### Authentication & Authorization
- JWT tokens issued by Identity Service
- API Gateway validates all requests
- Role-based access control (RBAC)
- Claims-based authorization in services

### Observability
**Distributed Tracing**
- OpenTelemetry instrumentation in all services
- Traces exported to Jaeger
- Correlation IDs for request tracking

**Logging**
- Structured logging with Serilog
- Logs aggregated in Elasticsearch
- Context enrichment with correlation IDs

**Metrics**
- OpenTelemetry metrics
- Custom business metrics
- Health checks for all services

### Resilience
**Polly Policies**
- Retry policy: 3 retries with exponential backoff
- Circuit breaker: Opens after 5 consecutive failures
- Timeout policy: 30s for external calls
- Fallback policy: Graceful degradation

### Security
- HTTPS for all external communication
- Secrets managed via Kubernetes Secrets
- Input validation with FluentValidation
- SQL injection prevention (parameterized queries)
- XSS protection with encoding

## Deployment Architecture

### Kubernetes
```
Namespace: distributed-commerce
├── API Gateway (2 replicas)
├── Identity Service (2 replicas)
├── Catalog Service (3 replicas) - higher load
├── Order Service (2 replicas)
├── Payment Service (2 replicas)
├── Inventory Service (2 replicas)
├── Shipping Service (2 replicas)
├── Notification Service (2 replicas)
└── Analytics Service (1 replica)

Namespace: infrastructure
├── PostgreSQL (HA setup)
├── Redis (cluster mode)
├── Kafka (3 brokers)
└── Zookeeper (3 nodes)

Namespace: monitoring
├── Jaeger
├── Elasticsearch
└── Prometheus
```

### Scaling Strategy
- Horizontal Pod Autoscaler (HPA) based on CPU/memory
- Kafka consumer groups for parallel processing
- Read replicas for PostgreSQL (high-read services)
- Redis cluster for distributed caching

## Design Patterns

### Domain-Driven Design (DDD)
- Bounded contexts per service
- Aggregates, entities, value objects
- Domain events for state changes
- Rich domain models

### CQRS (Command Query Responsibility Segregation)
- MediatR for command/query separation
- Separate read/write models where needed
- Optimized queries for read-heavy operations

### Event Sourcing
- Domain events as source of truth
- Event store in Kafka
- Event replay capability
- Audit trail built-in

### Saga Pattern
- Order Service as Saga Orchestrator
- Compensating transactions for rollback
- State machine for saga steps
- Persistent saga state in database

### Outbox Pattern
- Reliable event publishing
- Transactional outbox table
- Background worker publishes to Kafka
- At-least-once delivery guarantee

### Circuit Breaker
- Prevents cascading failures
- Fallback mechanisms
- Self-healing after timeout
- Implemented with Polly

## Consistency Model

### Eventual Consistency
- Services are eventually consistent via events
- Order-Inventory-Payment consistency via Saga
- Retry mechanisms for failed events
- Compensation for failed transactions

### Strong Consistency
- Within service boundaries (ACID transactions)
- Single database per service
- Optimistic concurrency with EF Core

## Failure Handling

### Retry Strategy
- Transient failures: Automatic retry with backoff
- Idempotency keys for safe retries
- Dead letter queue for failed messages

### Compensation
- Saga rollback via compensating transactions
- `InventoryReleased` compensates `InventoryReserved`
- `PaymentRefunded` compensates `PaymentProcessed`

### Monitoring & Alerts
- Health check endpoints on all services
- Liveness and readiness probes in K8s
- Alert on circuit breaker open state
- Alert on high error rates

## Security Architecture

### API Gateway Security
- Rate limiting per client/IP
- CORS policy enforcement
- Request/response validation
- DDoS protection

### Service-to-Service
- Mutual TLS (mTLS) in production
- Service mesh (Istio) for zero-trust
- Network policies in Kubernetes

### Data Security
- Encryption at rest (database level)
- Encryption in transit (TLS)
- PII data masking in logs
- GDPR compliance considerations

---

**Next**: See [LLD.md](LLD.md) for implementation details
