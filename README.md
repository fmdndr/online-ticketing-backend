# 🎫 Event Ticketing System — Microservices Architecture

A production-ready, event-driven microservices platform for online event ticketing built with **.NET 8**, showcasing CQRS, Saga (Choreography), distributed locking, and API gateway patterns.

---

## 🏗️ Architecture Overview

```
┌──────────────────────────────────────────────────────────────┐
│                     CLIENT / POSTMAN                         │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│              API GATEWAY (YARP) :5000                        │
│         Retry + Circuit Breaker (Polly)                      │
│              Serilog → Seq Logging                           │
└──────┬───────────────┬───────────────────┬───────────────────┘
       │               │                   │
       ▼               ▼                   ▼
┌─────────────┐ ┌─────────────┐ ┌──────────────────┐
│ Catalog.API │ │ Basket.API  │ │   Payment.API    │
│    :5001    │ │    :5002    │ │      :5003       │
│             │ │             │ │                  │
│  MongoDB    │ │   Redis     │ │   PostgreSQL     │
│  MediatR    │ │  SETNX Lock │ │   EF Core        │
│  CQRS       │ │  10min TTL  │ │   MassTransit    │
└──────┬──────┘ └──────┬──────┘ └────────┬─────────┘
       │               │                 │
       └───────────────┼─────────────────┘
                       │
              ┌────────▼────────┐
              │   RabbitMQ      │
              │ (Message Broker)│
              │   :5672/:15672  │
              └─────────────────┘
                       │
              ┌────────▼────────┐
              │    Seq          │
              │ (Logging)       │
              │   :5341/:8081   │
              └─────────────────┘
```

---

## 🛠️ Tech Stack

| Technology | Purpose | Why? |
|---|---|---|
| **.NET 8** | Runtime | Long-term support, high performance |
| **MongoDB** | Catalog DB | Schema-flexible for event/ticket data |
| **Redis** | Basket + Locking | In-memory speed for sessions; SETNX for distributed locks |
| **PostgreSQL** | Payment DB | ACID compliance for financial records |
| **RabbitMQ** | Message Broker | Reliable async messaging between services |
| **MassTransit** | Messaging Abstraction | Simplifies RabbitMQ integration with .NET |
| **MediatR** | CQRS Pattern | Clean separation of commands/queries |
| **YARP** | API Gateway | Microsoft's high-performance reverse proxy |
| **Polly** | Resilience | Retry + Circuit Breaker for fault tolerance |
| **Serilog + Seq** | Centralized Logging | Structured logs with searchable dashboard |
| **Docker Compose** | Infrastructure | One-command infrastructure setup |

---

## 📐 Architectural Decisions

### Why CQRS (Command Query Responsibility Segregation)?
The Catalog service separates **read operations** (queries) from **write operations** (commands) using MediatR. This provides:
- **Single Responsibility**: Each handler does one thing well
- **Scalability**: Read and write paths can be scaled independently
- **Testability**: Each handler is independently unit-testable

### Why Saga / Choreography?
We use the **Choreography-based Saga** pattern for the checkout flow:

```
Checkout → Basket publishes TicketReservedEvent
         → Payment consumes → processes payment
             ✅ Success → publishes PaymentCompletedEvent → Basket clears cart
             ❌ Failure → publishes PaymentFailedEvent → Basket releases lock (COMPENSATING)
```

**Why not Orchestration?** Choreography keeps services fully decoupled — no central orchestrator is a single point of failure. Each service reacts to events independently.

### Why Distributed Locking (Redis SETNX)?
When thousands of users try to book the same ticket simultaneously, we need **mutual exclusion**:
- `SETNX` (SET if Not eXists) atomically acquires a lock
- Lock has a **10-minute TTL** matching the basket expiry
- On payment failure, the **compensating transaction** releases the lock
- Safe release: only the lock holder can release it

---

## 🚀 Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

### 1. Start Infrastructure
```bash
docker compose up -d
```

This starts PostgreSQL, MongoDB, Redis, RabbitMQ, and Seq.

### 2. Run the Services
Open separate terminals for each:

```bash
# Terminal 1 — Gateway
dotnet run --project src/Gateway/Gateway.API --urls "http://localhost:5000"

# Terminal 2 — Catalog Service
dotnet run --project src/Services/Catalog/Catalog.API --urls "http://localhost:5001"

# Terminal 3 — Basket Service
dotnet run --project src/Services/Basket/Basket.API --urls "http://localhost:5002"

# Terminal 4 — Payment Service
dotnet run --project src/Services/Payment/Payment.API --urls "http://localhost:5003"
```

### 3. Verify
- **Swagger**: http://localhost:5001/swagger (Catalog), http://localhost:5002/swagger (Basket), http://localhost:5003/swagger (Payment)
- **RabbitMQ Dashboard**: http://localhost:15672 (guest/guest)
- **Seq Dashboard**: http://localhost:8081

---

## 📡 API Endpoints

All endpoints are accessible through the **Gateway** at `http://localhost:5000`.

### Catalog Service
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/catalog` | Get all events |
| GET | `/api/catalog/{id}` | Get event by ID |
| GET | `/api/catalog/category/{category}` | Get events by category |
| POST | `/api/catalog` | Create a new event |
| PUT | `/api/catalog` | Update an event |
| DELETE | `/api/catalog/{id}` | Delete an event |

### Basket Service
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/basket/{userId}` | Get user's basket |
| POST | `/api/basket` | Add/update items in basket |
| DELETE | `/api/basket/{userId}` | Clear basket |
| POST | `/api/basket/checkout` | Initiate checkout (publishes event) |

### Payment Service
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/payment` | Get all payment records |
| GET | `/api/payment/order/{orderId}` | Get payment by order ID |
| GET | `/api/payment/user/{userId}` | Get payments by user ID |

---

## 📁 Project Structure

```
EventTicketingSystem/
├── docker-compose.yml                      # Infrastructure containers
├── EventTicketingSystem.sln                # Solution file
├── src/
│   ├── Gateway/
│   │   └── Gateway.API/                   # YARP reverse proxy + Polly
│   ├── Services/
│   │   ├── Catalog/
│   │   │   └── Catalog.API/              # MongoDB, MediatR CQRS
│   │   │       ├── Controllers/
│   │   │       ├── Data/                 # MongoDB context + seeding
│   │   │       ├── Entities/
│   │   │       ├── Features/             # CQRS Commands & Queries
│   │   │       └── Repositories/
│   │   ├── Basket/
│   │   │   └── Basket.API/              # Redis, distributed lock
│   │   │       ├── Controllers/
│   │   │       ├── Consumers/           # MassTransit event handlers
│   │   │       └── Repositories/
│   │   └── Payment/
│   │       └── Payment.API/             # PostgreSQL, EF Core
│   │           ├── Controllers/
│   │           ├── Consumers/           # TicketReservedEvent handler
│   │           ├── Data/
│   │           └── Entities/
│   └── Shared/
│       └── Shared.Common/               # Shared models, DTOs, events
│           ├── DTOs/
│           ├── Events/
│           ├── Exceptions/
│           └── Models/
├── postman/
│   └── EventTicketingSystem.postman_collection.json
└── README.md
```

---

## 🧪 Testing the Full Flow

1. **Get events** → `GET /api/catalog` — Returns seeded events
2. **Copy an event ID** from the response
3. **Add to basket** → `POST /api/basket` with the event ID
4. **Checkout** → `POST /api/basket/checkout`
5. **Check payment** → `GET /api/payment/user/user-001`
6. **Check Seq logs** → http://localhost:8081 for the full event trace

---

## 🐳 Infrastructure Ports

| Service | Port | Credentials |
|---------|------|-------------|
| API Gateway | 5000 | — |
| Catalog API | 5001 | — |
| Basket API | 5002 | — |
| Payment API | 5003 | — |
| PostgreSQL | 5432 | postgres / postgres |
| MongoDB | 27017 | — |
| Redis | 6379 | — |
| RabbitMQ (AMQP) | 5672 | guest / guest |
| RabbitMQ (UI) | 15672 | guest / guest |
| Seq (Ingestion) | 5341 | — |
| Seq (Dashboard) | 8081 | — |

---

## 📬 Postman Collection

Import the collection from `postman/EventTicketingSystem.postman_collection.json` into Postman.

**Collection Variables:**
- `gateway_url`: `http://localhost:5000`
- `user_id`: `user-001`
- `event_id`: Set after creating/getting an event
- `order_id`: Set after checkout

---

## 📝 License

This project is for educational and demonstration purposes.
