# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Build entire solution
dotnet build

# Run all tests
dotnet test

# Run a specific test project
dotnet test tests/FCG.Payments.Domain.Tests

# Run a specific test class
dotnet test --filter ClassName=PaymentTransactionTests

# Run a single test by name
dotnet test --filter "FullyQualifiedName~Create_WithValidData_ShouldSetProperties"

# Test with coverage
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover
```

## Architecture

This is a .NET 8 payment processing system organized in layers:

- **FCG.Payments.Domain** — Entities with factory methods/validation, enums, domain events (records), repository interfaces. No dependencies.
- **FCG.Payments.Application** — IMessagePublisher abstraction. Depends on Domain.
- **FCG.Payments.Infrastructure** — EF Core DbContext, repository implementations, Azure Service Bus publisher. Depends on Application.
- **FCG.Payments.Functions** — Azure Functions (Isolated Worker) triggered by Service Bus queues. Depends on Application + Infrastructure. Sole entry point of the system.

### Message Flow

1. Games Service publishes `OrderPlacedEvent` → `order-placed` queue
2. `ProcessPaymentFunction` consumes → checks for duplicate (same user+game pending) → creates transaction → processes payment (even cents → Approved, odd → Rejected) → publishes `PaymentProcessedEvent` to `payments-processed`
3. Games Service consumes `PaymentProcessedEvent` → adds game to library or rejects
4. If duplicate detected, logs warning and skips processing

### Key Abstractions

- **IPaymentTransactionRepository** — Domain interface, implemented by Infrastructure
- **IMessagePublisher** — Application interface with `InMemoryMessagePublisher` (dev) and `ServiceBusMessagePublisher` (production, uses Azure.Messaging.ServiceBus)
- **PaymentsDbContext** — EF Core DbContext for PaymentTransaction persistence
- **PaymentTransaction** — Entity with `Create()` factory method

### Configuration

- `SQL_CONNECTION` — SQL Server/Azure SQL connection string (defaults to localhost SA)
- `SERVICEBUS_CONNECTION` — Azure Service Bus connection string (empty = in-memory bus)
- `APPLICATIONINSIGHTS_CONNECTION_STRING` — Application Insights (empty = disabled)
- EF Core uses `EnableRetryOnFailure` for Azure SQL Serverless cold starts

### Testing Patterns

- Tests follow AAA (Arrange-Act-Assert) with naming convention `MethodName_Condition_ExpectedBehavior`
- 3 test projects: Domain.Tests, Infrastructure.Tests, Functions.Tests
- Infrastructure tests use EF Core InMemory provider
- Functions tests mock repository and message publisher
- Domain tests validate entity factory methods and state transitions
