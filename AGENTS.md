# AGENTS.md

## Purpose
- This repo is a .NET 8 microservice demo for event ticketing with async checkout saga choreography.
- Optimize for fast local iteration across `Gateway`, `Catalog`, `Basket`, `Payment`, and `Shared.Common`.

## Repo Map (source of truth)
- `src/Gateway/Gateway.API`: YARP gateway routes `/api/catalog|basket|payment` to ports `5001|5002|5003` (`appsettings.json`, `Program.cs`).
- `src/Services/Catalog/Catalog.API`: Mongo-backed catalog; CQRS-style handlers under `Features/Events/*` using MediatR.
- `src/Services/Basket/Basket.API`: Redis basket storage plus ticket lock management (`SETNX` via `When.NotExists`).
- `src/Services/Payment/Payment.API`: EF Core + PostgreSQL payment records; consumes reservation events.
- `src/Shared/Shared.Common`: shared contracts (`DTOs/ApiResponse.cs`, `Events/*.cs`, `Models/*.cs`).

## Cross-Service Flow You Must Preserve
- Checkout flow is event-driven, not synchronous:
  1. `BasketController.Checkout` publishes `TicketReservedEvent` per basket item.
  2. `TicketReservedEventConsumer` in Payment simulates processing, writes `PaymentRecord`, then publishes `PaymentCompletedEvent` or `PaymentFailedEvent`.
  3. Basket consumers react: success clears basket; failure releases Redis locks (compensating transaction).
- Lock semantics matter for correctness: key format is `lock:ticket:{eventId}:{ticketTypeName}` and lock value is `userId` (`BasketRepository.cs`).

## Project Conventions (observed in code)
- API responses are wrapped with `ApiResponse<T>.Success/Fail` across controllers; keep this envelope consistent.
- Controllers use `[Route("api/[controller]")]`; gateway expects `/api/catalog`, `/api/basket`, `/api/payment` path families.
- Service startup pattern is minimal-host + DI in `Program.cs` + Serilog to console and Seq (`http://localhost:5341`).
- Catalog uses MediatR request/handler files colocated by feature (`Commands`, `Queries`); new catalog behavior should follow this split.
- Shared integration contracts live in `Shared.Common`; do not duplicate event/model definitions inside services.

## Developer Workflow (local)
- Infrastructure only:
```bash
cd /Users/fatihmahmutdundar/workspace/online-ticketing/othello-ticketing-backend
docker compose up -d
```
- Build everything:
```bash
dotnet build /Users/fatihmahmutdundar/workspace/online-ticketing/othello-ticketing-backend/EventTicketingSystem.sln
```
- Run services (separate terminals): gateway `5000`, catalog `5001`, basket `5002`, payment `5003` (see `README.md`).
- In Docker Compose, gateway is mapped to `5010` (macOS port `5000` is reserved by AirPlay/Control Center).
- Payment auto-applies EF migrations at startup (`Payment.API/Program.cs`); design-time factory exists for `dotnet ef` commands.

## Integration Endpoints and Tools
- Swagger: `http://localhost:5001/swagger`, `http://localhost:5002/swagger`, `http://localhost:5003/swagger`.
- RabbitMQ UI: `http://localhost:15672` (`guest/guest`), Seq UI: `http://localhost:8081`.
- Postman collection: `postman/EventTicketingSystem.postman_collection.json`.

## Agent Guardrails for Changes
- Prefer extending existing patterns over introducing new infrastructure abstractions.
- Keep message contract changes backward-compatible across consumers/publishers in different services.
- If you change routes, update both service controllers and gateway `ReverseProxy` config together.
- Note: README mentions Polly, but no active Polly policy configuration is present in service code; verify before documenting/adding resilience behavior.

