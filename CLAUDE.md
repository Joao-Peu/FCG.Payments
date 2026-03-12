# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Build entire solution
dotnet build

# Run the API
dotnet run --project src/FCG.Payments.Api

# Run all tests
dotnet test

# Run a specific test project
dotnet test tests/FCG.Payments.Application.Tests

# Run a specific test class
dotnet test --filter ClassName=CreatePaymentTransactionHandlerTests

# Run a single test by name
dotnet test --filter "FullyQualifiedName~Handle_WithValidData_ShouldCreateAndReturnResult"

# Test with coverage
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover
```

## Architecture

This is a .NET 8 payment processing system using **CQRS with MediatR**, organized in layers:

- **FCG.Payments.Domain** — Entities with factory methods/validation, enums, domain events (records), repository interfaces. No dependencies.
- **FCG.Payments.Application** — MediatR commands/queries with handlers, IMessagePublisher abstraction. Depends on Domain.
- **FCG.Payments.Infrastructure** — EF Core DbContext with audit trail, repository implementations, Azure Service Bus publisher, OpenTelemetry/Application Insights. Depends on Application.
- **FCG.Payments.Api** — ASP.NET Core Web API with controllers using IMediator. Depends on Application + Infrastructure.
- **FCG.Payments.Functions** — Azure Functions (Isolated Worker) triggered by Service Bus queues, using IMediator. Depends on Application + Infrastructure.

### Message Flow

1. Games Service publishes `OrderPlacedEvent` → `order-placed` queue
2. `ConsumeOrderPlacedFunction` → `CreatePaymentTransactionCommand` → saves transaction (Status=Created) → publishes to `payments-start`
3. `ProcessPaymentFunction` → `ProcessPaymentCommand` → deterministic logic (even cents → Paid, odd → Failed) → publishes `PaymentProcessedEvent` to `payments-processed`
4. Games Service consumes `PaymentProcessedEvent` → adds game to library or rejects

### CQRS Pattern

**Commands:**
- `CreatePaymentTransactionCommand` — Creates transaction, persists, publishes to payments-start
- `ProcessPaymentCommand` — Applies payment logic, updates status, publishes processed event

**Queries:**
- `GetPaymentByPurchaseIdQuery` — Single transaction lookup by purchase ID
- `QueryPaymentTransactionsQuery` — Filtered list (userId, date range, status)

### Key Abstractions

- **IPaymentTransactionRepository** — Domain interface, implemented by Infrastructure
- **IMessagePublisher** — Application interface with `InMemoryMessagePublisher` (dev) and `ServiceBusMessagePublisher` (production, uses Azure.Messaging.ServiceBus)
- **PaymentsDbContext** — Automatic audit trail via `SaveChanges` interception
- **PaymentTransaction** — Rich entity with `Create()` factory and `UpdateStatus()` with state machine validation

### Configuration

- `SQL_CONNECTION` — SQL Server/Azure SQL connection string (defaults to localhost SA)
- `SERVICEBUS_CONNECTION` — Azure Service Bus connection string (empty = in-memory bus)
- `APPLICATIONINSIGHTS_CONNECTION_STRING` — Application Insights (empty = disabled)
- JWT Bearer authentication configured with all validation disabled in dev
- Health checks at `/health` and `/ready`
- EF Core uses `EnableRetryOnFailure` for Azure SQL Serverless cold starts

### Testing Patterns

- Tests follow AAA (Arrange-Act-Assert) with naming convention `MethodName_Condition_ExpectedBehavior`
- 5 test projects mirror the 5 source projects
- Application tests mock `IPaymentTransactionRepository` + `IMessagePublisher`
- Infrastructure tests use EF Core InMemory provider
- Api tests mock `IMediator`
- Functions tests mock `IMediator`
- Domain tests validate entity factory methods and state transitions
