# Architecture

## System Overview

DistributedCommerce is a microservices-based e-commerce platform using event-driven architecture. Services communicate asynchronously via Kafka and synchronously through HTTP, all accessed via YARP API Gateway.

## High-Level Architecture

```
                    ┌───────────────────────────────────────────────┐
                    │          Clients (Web, Mobile, APIs)          │
                    └────────────────────┬──────────────────────────┘
                                         │ HTTPS
                                         ▼
                    ┌─────────────────────────────────────────────────────────┐
                    │              API Gateway (YARP 2.1.0)                   │
                    │  • Reverse Proxy  • JWT Auth  • Rate Limiting  • CORS   │
                    │  • Health Checks  • Request Routing  • Transformation   │
                    └────────────────────┬────────────────────────────────────┘
                                         │ HTTP (Internal)
              ┌──────────────────────────┼──────────────────────────┐
              │                          │                          │
              ▼                          ▼                          ▼
    ┌─────────────────┐      ┌─────────────────┐      ┌─────────────────┐
    │  Identity (x2)  │      │  Catalog (x3)   │      │   Order (x2)    │
    │  PostgreSQL     │      │  PostgreSQL     │      │  PostgreSQL     │
    │  + Redis Cache  │      │  + Redis Cache  │      │  + Saga State   │
    └────────┬────────┘      └────────┬────────┘      └────────┬────────┘
             │                        │                        │
    ┌────────┴────────┐      ┌────────┴────────┐      ┌────────┴────────┐
    │  Payment (x2)   │      │ Inventory (x2)  │      │  Shipping (x2)  │
    │  PostgreSQL     │      │  PostgreSQL     │      │  PostgreSQL     │
    │  + Idempotency  │      │  + Reservations │      │  + Tracking     │
    └────────┬────────┘      └────────┬────────┘      └────────┬────────┘
             │                        │                        │
    ┌────────┴────────┐      ┌────────┴────────────────────────┘
    │Notification(x2) │      │     Analytics (x1)
    │  PostgreSQL     │      │     PostgreSQL
    │  + Templates    │      │     + Aggregations
    └────────┬────────┘      └────────┬────────┘
             │                        │
             └────────────────┬───────┴───────────────────────┐
                              ▼                               │
                ┌─────────────────────────────┐               │
                │     Apache Kafka (x3)       │               │
                │  • Event Bus & Streaming    │               │
                │  • Topics per Domain        │               │
                │  • Consumer Groups          │               │
                │  • Schema Registry (Avro)   │               │
                └─────────────────────────────┘               │
                              │                               │
        ┌─────────────────────┼───────────────────┐           │
        ▼                     ▼                   ▼           │
┌───────────────┐     ┌───────────────┐   ┌──────────────┐   │
│  PostgreSQL   │     │ Redis Cluster │   │  Zookeeper   │   │
│   (HA Setup)  │     │  (Caching +   │   │    (x3)      │   │
│ Per-Service DB│     │ Rate Limiting)│   │              │   │
└───────────────┘     └───────────────┘   └──────────────┘   │
                                                              │
        ┌─────────────────────────────────────────────────────┘
        │              BuildingBlocks (Shared Libraries)
        ├─────────────────────────────────────────────────────┐
        │ • EventBus      • Saga         • Resilience         │
        │ • Idempotency   • Observability• Authentication     │
        │ • Domain        • Application  • Infrastructure     │
        └─────────────────────────────────────────────────────┘
                                │
        ┌───────────────────────┴───────────────────────┐
        ▼                                               ▼
┌────────────────────────────┐          ┌─────────────────────────────┐
│   Observability Stack      │          │   Infrastructure Support    │
├────────────────────────────┤          ├─────────────────────────────┤
│ • Jaeger (Tracing)         │          │ • Kubernetes (Orchestration)│
│ • Prometheus (Metrics)     │          │ • Ingress Controllers       │
│ • Grafana (Visualization)  │          │ • ConfigMaps & Secrets      │
│ • Loki (Logging)           │          │ • Network Policies          │
│ • OpenTelemetry Collector  │          │ • HPA (Auto-scaling)        │
│ • Elasticsearch (Logs)     │          │ • PersistentVolumes         │
└────────────────────────────┘          └─────────────────────────────┘
```

## Event Flow Architecture

### Order Creation Saga Flow (Event-Driven)

```
┌─────────────┐
│   Client    │
└──────┬──────┘
       │ POST /api/orders
       ▼
┌─────────────────────────────────────────────────────────────────┐
│                      API Gateway → Order Service                │
│                     CreateOrder Command (HTTP)                  │
└──────────────────────────────┬──────────────────────────────────┘
                               │
                               ▼
                    ┌──────────────────────┐
                    │ Order Service (Saga  │
                    │    Orchestrator)     │
                    └──────┬───────────────┘
                           │ Publish: OrderCreated
                           ▼
                 ┌──────────────────────┐
                 │   Kafka Topic:       │
                 │   order-events       │
                 └──────┬───────────────┘
                        │
        ┌───────────────┼───────────────────────┐
        │               │                       │
        ▼               ▼                       ▼
┌──────────────┐ ┌──────────────┐      ┌──────────────┐
│  Inventory   │ │  Analytics   │      │ Notification │
│   Service    │ │   Service    │      │   Service    │
└──────┬───────┘ └──────────────┘      └──────────────┘
       │ Reserve Stock
       ▼
  [SAGA STEP 1: Reserve Inventory]
       │ Publish: InventoryReserved
       ▼
┌──────────────────────┐
│   Kafka Topic:       │
│   inventory-events   │
└──────┬───────────────┘
       │ Consumed by Order Service
       ▼
┌──────────────────────┐
│  Order Service       │
│  (Saga Continue)     │
└──────┬───────────────┘
       │ Publish: ProcessPayment
       ▼
┌──────────────────────┐
│   Kafka Topic:       │
│   payment-commands   │
└──────┬───────────────┘
       │
       ▼
┌──────────────┐
│   Payment    │
│   Service    │
└──────┬───────┘
       │ Process Payment (Idempotent)
       ▼
  [SAGA STEP 2: Process Payment]
       │ Publish: PaymentProcessed
       ▼
┌──────────────────────┐
│   Kafka Topic:       │
│   payment-events     │
└──────┬───────────────┘
       │ Consumed by Order Service
       ▼
┌──────────────────────┐
│  Order Service       │
│  (Saga Continue)     │
└──────┬───────────────┘
       │ Publish: CreateShipment
       ▼
┌──────────────────────┐
│   Kafka Topic:       │
│   shipping-commands  │
└──────┬───────────────┘
       │
       ▼
┌──────────────┐
│   Shipping   │
│   Service    │
└──────┬───────┘
       │ Create Shipment
       ▼
  [SAGA STEP 3: Create Shipment]
       │ Publish: ShipmentCreated
       ▼
┌──────────────────────┐
│   Kafka Topic:       │
│   shipping-events    │
└──────┬───────────────┘
       │ Consumed by Order Service
       ▼
┌──────────────────────┐
│  Order Service       │
│  [SAGA COMPLETED]    │
│  Status: Confirmed   │
└──────┬───────────────┘
       │ Publish: OrderConfirmed
       ▼
┌──────────────────────┐
│   Kafka Topic:       │
│   order-events       │
└──────┬───────────────┘
       │
       ├──────────────────────┬──────────────────┐
       ▼                      ▼                  ▼
┌──────────────┐      ┌──────────────┐   ┌──────────────┐
│ Notification │      │  Analytics   │   │   Customer   │
│   Service    │      │   Service    │   │   Frontend   │
│ (Send Email) │      │ (Track Sale) │   │  (WebSocket) │
└──────────────┘      └──────────────┘   └──────────────┘

──────────────────────────────────────────────────────────────────
                    COMPENSATION FLOW (On Failure)
──────────────────────────────────────────────────────────────────

  If Payment Fails:
      │
      ├─→ Publish: PaymentFailed
      │
      ▼
  Order Service (Saga Compensate)
      │
      ├─→ Publish: ReleaseInventory (Compensation)
      │
      ▼
  Inventory Service
      │
      └─→ Release Reserved Stock
      │
      ▼
  [SAGA COMPENSATED] → Order Status: Cancelled
```

### Kafka Topics & Consumer Groups

```
┌─────────────────────────────────────────────────────────────┐
│                        Kafka Cluster                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Topic: identity-events                                     │
│    ├─ Partition 0: UserRegistered, UserLoggedIn             │
│    └─ Consumer Group: analytics-consumers                   │
│                                                             │
│  Topic: catalog-events                                      │
│    ├─ Partition 0-2: ProductCreated, PriceChanged           │
│    └─ Consumer Groups: analytics-consumers, search-indexer  │
│                                                             │
│  Topic: order-events                                        │
│    ├─ Partition 0-2: OrderCreated, OrderConfirmed           │
│    └─ Consumer Groups: analytics-consumers, notification-   │
│                       consumers, shipping-consumers         │
│                                                             │
│  Topic: payment-events                                      │
│    ├─ Partition 0-1: PaymentProcessed, PaymentFailed        │
│    └─ Consumer Groups: order-saga-consumers, analytics-     │
│                                                             │
│  Topic: inventory-events                                    │
│    ├─ Partition 0-1: InventoryReserved, StockLevelChanged   │
│    └─ Consumer Groups: order-saga-consumers, analytics-     │
│                                                             │
│  Topic: shipping-events                                     │
│    ├─ Partition 0-1: ShipmentCreated, ShipmentDelivered     │
│    └─ Consumer Groups: order-saga-consumers, notification-  │
│                                                             │
│  Topic: notification-events                                 │
│    ├─ Partition 0: NotificationSent, NotificationFailed     │
│    └─ Consumer Groups: analytics-consumers                  │
│                                                             │
│  Schema Registry (Avro Schemas)                             │
│    └─ Version control for all event schemas                 │
│                                                             │
└─────────────────────────────────────────────────────────────┘
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

### Communication Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    SYNCHRONOUS COMMUNICATION                    │
│                         (HTTP/REST)                             │
└─────────────────────────────────────────────────────────────────┘

    Client Request (JWT Token)
           │
           ▼
    ┌──────────────┐
    │ API Gateway  │ ◄── Rate Limiter (100 req/min)
    │   (YARP)     │ ◄── JWT Validation
    │              │ ◄── CORS Policy
    └──────┬───────┘
           │
           ├─→ Route: /api/identity/**  → Identity Service
           ├─→ Route: /api/catalog/**   → Catalog Service  ◄── Redis Cache
           ├─→ Route: /api/orders/**    → Order Service
           ├─→ Route: /api/payments/**  → Payment Service  ◄── Idempotency
           ├─→ Route: /api/inventory/** → Inventory Service
           └─→ Route: /api/shipping/**  → Shipping Service
           
    Each request includes:
    • Authorization Header (Bearer token)
    • Correlation-Id (distributed tracing)
    • Idempotency-Key (for mutations)
    
    Resilience (Polly Policies):
    • Retry: 3 attempts, exponential backoff
    • Circuit Breaker: Open after 5 failures, 30s timeout
    • Timeout: 30s per request

┌─────────────────────────────────────────────────────────────────┐
│                   ASYNCHRONOUS COMMUNICATION                    │
│                    (Event-Driven via Kafka)                     │
└─────────────────────────────────────────────────────────────────┘

    Producer Service                 Kafka Broker              Consumer Service
         │                                │                           │
         │ 1. Domain Event Occurs         │                           │
         │    (e.g., OrderCreated)        │                           │
         │                                │                           │
         │ 2. Save to Outbox Table        │                           │
         │    (Transactional)             │                           │
         │                                │                           │
         │ 3. Outbox Worker publishes     │                           │
         ├────────────────────────────────►                           │
         │    Event to Kafka Topic        │                           │
         │    • Key: AggregateId          │                           │
         │    • Value: Event Payload      │                           │
         │    • Headers: Metadata         │                           │
         │                                │ 4. Consumer polls topic   │
         │                                ├──────────────────────────►│
         │                                │                           │
         │                                │ 5. Process Event          │
         │                                │    (Idempotent)           │
         │                                │                           │
         │                                │ 6. Commit Offset          │
         │                                │◄──────────────────────────┤
         │                                │                           │
         
    Event Flow Guarantees:
    • At-least-once delivery (via Outbox pattern)
    • Ordering per partition (same AggregateId)
    • Idempotent processing
    • Automatic retry on failure
    • Dead Letter Queue (DLQ) for poison messages

┌─────────────────────────────────────────────────────────────────┐
│              SERVICE-TO-SERVICE DEPENDENCIES                    │
└─────────────────────────────────────────────────────────────────┘

    Order Service (Saga Orchestrator)
        │
        ├─→ Publishes: ReserveInventory     → Inventory Service
        │                                      │
        ├─→ Subscribes: InventoryReserved   ◄──┘
        │
        ├─→ Publishes: ProcessPayment       → Payment Service
        │                                      │
        ├─→ Subscribes: PaymentProcessed    ◄──┘
        │
        ├─→ Publishes: CreateShipment       → Shipping Service
        │                                      │
        └─→ Subscribes: ShipmentCreated     ◄──┘

    Notification Service (Event Consumer)
        │
        ├─→ Subscribes: OrderConfirmed      ← Order Service
        ├─→ Subscribes: PaymentProcessed    ← Payment Service
        ├─→ Subscribes: ShipmentDelivered   ← Shipping Service
        └─→ Subscribes: UserRegistered      ← Identity Service

    Analytics Service (Event Consumer)
        │
        └─→ Subscribes: ALL domain events from ALL services
            • Real-time metrics aggregation
            • Business intelligence
            • Dashboards and reports
```

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

### Kubernetes Cluster Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        KUBERNETES CLUSTER                               │
│                    (Multi-Namespace Architecture)                       │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│  NAMESPACE: distributed-commerce (Application Layer)                    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────────────────┐         ┌──────────────────────┐               │
│  │ API Gateway         │         │  Ingress Controller  │               │
│  │ Deployment: 2       │◄────────│  (NGINX/Traefik)     │               │
│  │ HPA: 2-10           │         │  • SSL Termination   │               │
│  │ Service: ClusterIP  │         │  • Load Balancing    │               │
│  │ Port: 80            │         └──────────────────────┘               │
│  └─────────────────────┘                                                │
│           │                                                             │
│           ├──────────────┬──────────────┬────────────────┐              │
│           ▼              ▼              ▼                ▼              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │
│  │ Identity    │  │ Catalog     │  │ Order       │  │ Payment     │     │
│  │ Deploy: 2   │  │ Deploy: 3   │  │ Deploy: 2   │  │ Deploy: 2   │     │
│  │ HPA: 2-10   │  │ HPA: 3-20   │  │ HPA: 2-15   │  │ HPA: 2-10   │     │
│  │ CPU: 250m   │  │ CPU: 250m   │  │ CPU: 250m   │  │ CPU: 250m   │     │
│  │ Mem: 512Mi  │  │ Mem: 512Mi  │  │ Mem: 512Mi  │  │ Mem: 512Mi  │     │
│  │             │  │             │  │ + Saga      │  │ + Idempot.  │     │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘     │
│                                                                         │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │
│  │ Inventory   │  │ Shipping    │  │Notification │  │ Analytics   │     │
│  │ Deploy: 2   │  │ Deploy: 2   │  │ Deploy: 2   │  │ Deploy: 1   │     │
│  │ HPA: 2-10   │  │ HPA: 2-10   │  │ HPA: 2-8    │  │ (No HPA)    │     │
│  │ CPU: 250m   │  │ CPU: 250m   │  │ CPU: 250m   │  │ CPU: 500m   │     │
│  │ Mem: 512Mi  │  │ Mem: 512Mi  │  │ Mem: 512Mi  │  │ Mem: 1Gi    │     │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘     │
│                                                                         │
│  ConfigMaps: shared-config (env variables)                              │
│  Secrets: shared-secrets (DB passwords, JWT keys)                       │
│  NetworkPolicy: Allow only from API Gateway                             │
│  PodDisruptionBudget: MinAvailable=1 for all services                   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│  NAMESPACE: infrastructure (Data & Messaging Layer)                     │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  PostgreSQL (High Availability)                                         │
│  ┌──────────────────────────────────────────────────────┐               │
│  │ StatefulSet: postgres-primary (1 replica)            │               │
│  │ StatefulSet: postgres-replica (2 read replicas)      │               │
│  │ PVC: 50Gi per instance (SSD)                         │               │
│  │ Service: postgres-service (ClusterIP)                │               │
│  │ Per-Service Databases:                               │               │
│  │   • identity_db    • catalog_db    • order_db        │               │
│  │   • payment_db     • inventory_db  • shipping_db     │               │
│  │   • notification_db • analytics_db                   │               │
│  └──────────────────────────────────────────────────────┘               │
│                                                                         │
│  Redis (Cluster Mode)                                                   │
│  ┌──────────────────────────────────────────────────────┐               │
│  │ StatefulSet: redis-cluster (6 nodes)                 │               │
│  │   • 3 Master nodes                                   │               │
│  │   • 3 Replica nodes                                  │               │
│  │ PVC: 10Gi per node                                   │               │
│  │ Service: redis-service (Headless)                    │               │
│  │ Use Cases:                                           │               │
│  │   • Distributed Cache                                │               │
│  │   • Session Storage                                  │               │
│  │   • Rate Limiting Counters                           │               │
│  └──────────────────────────────────────────────────────┘               │
│                                                                         │
│  Kafka Cluster                                                          │
│  ┌──────────────────────────────────────────────────────┐               │
│  │ StatefulSet: kafka-broker (3 replicas)               │               │
│  │ StatefulSet: zookeeper (3 replicas)                  │               │
│  │ PVC: 100Gi per broker                                │               │
│  │ Service: kafka-service (Headless)                    │               │
│  │ Schema Registry: schema-registry (1 replica)         │               │
│  │ Topics: identity, catalog, order, payment,           │               │
│  │         inventory, shipping, notification            │               │
│  └──────────────────────────────────────────────────────┘               │
│                                                                         │
│  NetworkPolicy: Restricted to distributed-commerce namespace            │
│  ResourceQuota: CPU=10 cores, Memory=32Gi                               │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│  NAMESPACE: monitoring (Observability Stack)                            │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Jaeger (Distributed Tracing)                                           │
│  ┌──────────────────────────────────────────────────────┐               │
│  │ Deployment: jaeger-all-in-one (1 replica)            │               │
│  │ Service: jaeger-service (LoadBalancer)               │               │
│  │ Ports: 16686 (UI), 4317 (OTLP gRPC)                  │               │
│  │ Storage: Elasticsearch backend                       │               │
│  └──────────────────────────────────────────────────────┘               │
│                                                                         │
│  Prometheus (Metrics Collection)                                        │
│  ┌──────────────────────────────────────────────────────┐               │
│  │ StatefulSet: prometheus (1 replica)                  │               │
│  │ PVC: 50Gi (retention: 30 days)                       │               │
│  │ Service: prometheus-service (ClusterIP)              │               │
│  │ ServiceMonitor: Auto-discovers all services          │               │
│  └──────────────────────────────────────────────────────┘               │
│                                                                         │
│  Grafana (Visualization)                                                │
│  ┌──────────────────────────────────────────────────────┐               │
│  │ Deployment: grafana (2 replicas)                     │               │
│  │ Service: grafana-service (LoadBalancer)              │               │
│  │ Port: 3000 (HTTP)                                    │               │
│  │ Dashboards: Pre-provisioned (15+ dashboards)         │               │
│  │ DataSources: Prometheus, Loki, Jaeger                │               │
│  └──────────────────────────────────────────────────────┘               │
│                                                                         │
│  Loki (Log Aggregation)                                                 │
│  ┌──────────────────────────────────────────────────────┐               │
│  │ StatefulSet: loki (1 replica)                        │               │
│  │ PVC: 100Gi                                           │               │
│  │ Promtail: DaemonSet (runs on every node)             │               │
│  └──────────────────────────────────────────────────────┘               │
│                                                                         │
│  OpenTelemetry Collector                                                │
│  ┌──────────────────────────────────────────────────────┐               │
│  │ Deployment: otel-collector (2 replicas)              │               │
│  │ Receivers: OTLP, Jaeger, Prometheus                  │               │
│  │ Exporters: Jaeger, Prometheus, Loki                  │               │
│  └──────────────────────────────────────────────────────┘               │
│                                                                         │
│  Elasticsearch (Logs Storage)                                           │
│  ┌──────────────────────────────────────────────────────┐               │
│  │ StatefulSet: elasticsearch (3 replicas)              │               │
│  │ PVC: 100Gi per node                                  │               │
│  │ Retention: 7 days                                    │               │
│  └──────────────────────────────────────────────────────┘               │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│  KUSTOMIZE OVERLAYS (Environment-Specific Configs)                      │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  base/                  # Base configuration (shared)                   │
│    ├── deployment.yaml                                                  │
│    ├── service.yaml                                                     │
│    ├── configmap.yaml                                                   │
│    └── kustomization.yaml                                               │
│                                                                         │
│  overlays/dev/          # Development environment                       │
│    ├── replicas: 1                                                      │
│    ├── resources: minimal                                               │
│    └── ingress: dev.domain.com                                          │
│                                                                         │
│  overlays/staging/      # Staging environment                           │
│    ├── replicas: 2                                                      │
│    ├── resources: moderate                                              │
│    └── ingress: staging.domain.com                                      │
│                                                                         │
│  overlays/production/   # Production environment                        │
│    ├── replicas: 3+ (with HPA)                                          │
│    ├── resources: production-grade                                      │
│    ├── ingress: api.domain.com                                          │
│    └── security: network policies, pod security                         │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
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
