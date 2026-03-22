# FCG.Payments

Microsserviço de processamento de pagamentos para a plataforma FCG (FIAP Cloud Games). Processa pagamentos de compra de jogos de forma assíncrona através de Azure Functions com trigger de Azure Service Bus. Projeto da **Fase 3 do Tech Challenge — PosTech FIAP**.

## Fluxo de Comunicação entre Microsserviços

```mermaid
graph LR
    Client([Cliente]) -->|HTTP| APIM[API Gateway]
    APIM -->|/api/users/**| Users[FGC.Users API]
    APIM -->|/api/games/**| Games[FCG.Games API]

    Games -->|OrderPlacedEvent| Q1[/order-placed/]
    Q1 -->|ServiceBusTrigger| Payments[FCG.Payments Function]
    Payments -->|PaymentProcessedEvent| Q2[/payments-processed/]
    Q2 -->|BackgroundService| Games
    Payments -->|PaymentProcessedEvent| Q3[/notifications-payment-processed/]
    Q3 -->|ServiceBusTrigger| Notifications[FGC.Notifications Function]

    Users --- DB1[(FGCUsersDb)]
    Games --- DB2[(FCGGamesDb)]
    Payments --- DB3[(FCGPaymentsDb)]

    Games -.->|Logs & Traces| AI[Application Insights]
    Users -.->|Logs & Traces| AI
    Payments -.->|Logs & Traces| AI
    Notifications -.->|Logs & Traces| AI
```

## Fluxo de Mensagens

```mermaid
sequenceDiagram
    participant GS as FCG.Games API
    participant Q1 as Queue: order-placed
    participant PF as ProcessPaymentFunction
    participant DB as SQL Server
    participant Q2 as Queue: payments-processed
    participant Q3 as Queue: notifications-payment-processed

    GS->>Q1: OrderPlacedEvent (com CorrelationId)
    Q1->>PF: ServiceBusTrigger (lê CorrelationId)
    PF->>DB: Verifica duplicidade (ExistsPendingAsync)

    alt Pagamento duplicado
        PF-->>PF: Log warning + skip
    else Novo pagamento
        Note over PF: Centavos pares = Approved<br/>Centavos ímpares = Rejected
        PF->>DB: Salva PaymentTransaction
        PF->>Q2: PaymentProcessedEvent (com CorrelationId)
        PF->>Q3: PaymentProcessedEvent (com CorrelationId)
    end

    Q2->>GS: Consumer recebe resultado
```

## Diagrama de Arquitetura

```mermaid
graph TB
    subgraph "FCG.Payments - Microsserviço de Pagamentos"
        subgraph "Azure Functions Layer"
            PPF[ProcessPaymentFunction]
            TR[ServiceBusTrigger]
        end

        subgraph "Application Layer"
            MP[IMessagePublisher]
            ME[MessageEnvelope]
        end

        subgraph "Domain Layer"
            PT[PaymentTransaction Entity]
            PS[PaymentStatus Enum]
            OPE[OrderPlacedEvent]
            PPE[PaymentProcessedEvent]
        end

        subgraph "Infrastructure Layer"
            REPO[PaymentTransactionRepository]
            SBP[ServiceBusMessagePublisher]
            PDBC[PaymentsDbContext]
            DB[(SQL Server)]
        end
    end

    SBIn[/Queue: order-placed\] --> TR --> PPF
    PPF --> REPO & SBP
    REPO --> PDBC --> DB
    SBP --> SBOut1[/Queue: payments-processed\]
    SBP --> SBOut2[/Queue: notifications-payment-processed\]
```

## Arquitetura

O projeto segue **Clean Architecture** com Azure Functions Isolated Worker (.NET 8):

```
src/
├── FCG.Payments.Domain/           # Entidades, Enums, Eventos, Interfaces (zero dependências)
├── FCG.Payments.Application/      # IMessagePublisher, MessageEnvelope
├── FCG.Payments.Infrastructure/   # EF Core, Service Bus Publisher, Repositórios
└── FCG.Payments.Functions/        # Azure Functions (ServiceBusTrigger)
tests/
├── FCG.Payments.Domain.Tests/          # Testes de entidade PaymentTransaction
├── FCG.Payments.Infrastructure.Tests/  # Testes do repositório com EF InMemory
└── FCG.Payments.Functions.Tests/       # Testes da Function com mocks
```

**Fluxo de dependências:** Domain ← Application ← Infrastructure; Functions → Application + Infrastructure

## Regra de Negócio

O processamento de pagamento usa uma regra determinística para simulação:

| Centavos do preço | Status |
|-------------------|--------|
| Pares (ex: R$ 59.**90**) | `Approved` |
| Ímpares (ex: R$ 49.**99**) | `Rejected` |

## Rastreamento Distribuído (Correlation ID)

O `CorrelationId` é propagado de ponta a ponta:

1. **ProcessPaymentFunction** recebe `ServiceBusReceivedMessage` e lê `CorrelationId` da mensagem
2. Cria uma `Activity` vinculada ao correlation ID original para manter o trace
3. Enriquece todos os logs com `CorrelationId` via `ILogger.BeginScope`
4. Repassa o `CorrelationId` no `MessageEnvelope` ao publicar `PaymentProcessedEvent`

## Configuração

| Variável | Descrição | Padrão |
|----------|-----------|--------|
| `SQL_CONNECTION` | Connection string do SQL Server | `localhost SA` |
| `SERVICEBUS_CONNECTION` | Connection string do Azure Service Bus | (InMemory se vazio) |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Application Insights | (desabilitado se vazio) |

## CI/CD

Pipeline GitHub Actions (`.github/workflows/ci-cd.yml`):

- **CI** (push + PR na master): restore → build → test
- **CD** (apenas push na master): build Docker → push ACR → deploy Azure Container App

## Build & Run

```bash
# Build
dotnet build

# Rodar testes (22 testes)
dotnet test

# Rodar Functions localmente (requer Azure Functions Core Tools)
cd src/FCG.Payments.Functions
func start
```

## Docker

```bash
docker build -f src/FCG.Payments.Functions/Dockerfile -t fcg-payments .
docker run -p 5098:80 \
  -e SQL_CONNECTION="Server=tcp:..." \
  -e SERVICEBUS_CONNECTION="Endpoint=sb://..." \
  fcg-payments
```

## Testes

22 testes com xUnit + Moq:

| Projeto | Testes |
|---------|--------|
| Domain (PaymentTransaction) | 12 |
| Infrastructure (Repository + EF InMemory) | 6 |
| Functions (ProcessPaymentFunction) | 4 |

## Observabilidade

- **Serilog** com sinks para Console e Application Insights
- **Correlation ID** propagado entre mensagens do Service Bus para rastreamento distribuído
- **Application Insights** para logs, traces e métricas centralizados

## Tecnologias

- .NET 8.0
- Azure Functions (Isolated Worker)
- Azure Service Bus (Queues)
- Entity Framework Core 8 (SQL Server)
- Serilog + Application Insights
- xUnit + Moq
