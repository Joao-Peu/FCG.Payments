# FCG.Payments

Sistema de processamento de pagamentos para a plataforma FCG (Fiap Cloud Gaming). Processa pagamentos de compra de jogos através de integração com Azure Service Bus.

## Arquitetura

O projeto segue **CQRS** com **MediatR** e está organizado em camadas:

```
src/
├── FCG.Payments.Domain          # Entidades, enums, eventos, interfaces
├── FCG.Payments.Application     # Commands, Queries, Handlers (MediatR)
├── FCG.Payments.Infrastructure  # EF Core, Service Bus, Telemetry
├── FCG.Payments.Api             # ASP.NET Core Web API
└── FCG.Payments.Functions       # Azure Functions (Service Bus triggers)

tests/
├── FCG.Payments.Domain.Tests
├── FCG.Payments.Application.Tests
├── FCG.Payments.Infrastructure.Tests
├── FCG.Payments.Api.Tests
└── FCG.Payments.Functions.Tests
```

## Fluxo de Mensagens

```
Games Service → OrderPlacedEvent → queue "order-placed"
                                        ↓
                              ConsumeOrderPlacedFunction
                              → CreatePaymentTransactionCommand
                              → salva transação (Status=Created)
                              → publica → queue "payments-start"
                                                ↓
                                  ProcessPaymentFunction
                                  → ProcessPaymentCommand
                                  → centavos pares = Paid, ímpares = Failed
                                  → publica PaymentProcessedEvent
                                        ↓
Games Service ← queue "payments-processed"
  Se Paid → adiciona jogo à biblioteca
  Se Failed → rejeita
```

**Filas Service Bus:**
- `order-placed` — entrada (Games → Payments)
- `payments-start` — interna (API → ProcessPayment Function)
- `payments-processed` — saída (Payments → Games)
- `notifications` — interna (stub para notificações)

## Configuração

| Variável | Descrição | Padrão |
|----------|-----------|--------|
| `SQL_CONNECTION` | Connection string do SQL Server/Azure SQL | localhost SA |
| `SERVICEBUS_CONNECTION` | Connection string do Azure Service Bus | (in-memory se vazio) |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Connection string do Application Insights | (desabilitado se vazio) |

## Desenvolvimento Local

### Pré-requisitos
- .NET 8.0 SDK
- SQL Server (ou Docker)
- Azure Functions Core Tools (para Functions)

### Build e Testes

```bash
# Build
dotnet build

# Rodar todos os testes
dotnet test

# Rodar testes de um projeto específico
dotnet test tests/FCG.Payments.Application.Tests

# Rodar um teste específico
dotnet test --filter "FullyQualifiedName~CreatePaymentTransactionHandlerTests"

# Testes com cobertura
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover
```

### Executar API

```bash
dotnet run --project src/FCG.Payments.Api
```

A API estará disponível em `http://localhost:5098` com Swagger UI.

### Docker

```bash
# Build API
docker build -f src/FCG.Payments.Api/Dockerfile -t fcg-payments-api .

# Build Functions
docker build -f src/FCG.Payments.Functions/Dockerfile -t fcg-payments-functions .
```

## API Endpoints

| Método | Rota | Descrição |
|--------|------|-----------|
| `POST` | `/api/payments` | Criar pagamento |
| `GET` | `/api/payments/{purchaseId}` | Consultar status do pagamento |
| `GET` | `/api/payments/transactions` | Listar transações (com filtros) |
| `GET` | `/health` | Health check |
| `GET` | `/ready` | Readiness check |

## Tecnologias

- .NET 8.0
- ASP.NET Core Web API
- Azure Functions (Isolated Worker)
- Azure Service Bus
- Azure SQL (Serverless)
- Azure Monitor (Application Insights + OpenTelemetry)
- Entity Framework Core 8
- MediatR 12
- xUnit + Moq
