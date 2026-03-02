# Guia de Testes Unitários - FCG.Payments

## Visão Geral

O projeto agora possui uma suite completa de testes unitários seguindo as melhores práticas de Clean Architecture. Os testes garantem a confiabilidade do código e facilitam refatorações futuras.

## Estrutura de Testes

```
FCG.Payments.Tests/
├── Services/
│   └── PaymentServiceTests.cs         # Testes do serviço de pagamentos
├── Controllers/
│   └── PaymentsControllerTests.cs     # Testes dos endpoints
└── FCG.Payments.Tests.csproj          # Projeto de testes
```

## Ferramentas Utilizadas

- **xUnit**: Framework de testes de fácil uso e moderno
- **Moq**: Biblioteca para criar mocks e stubs
- **Entity Framework In-Memory**: Para testar a camada de dados sem banco real

## Como Executar os Testes

### Terminal/CLI

```bash
# Executar todos os testes
dotnet test

# Executar apenas um projeto de testes
dotnet test FCG.Payments.Tests/FCG.Payments.Tests.csproj

# Executar com verbosidade
dotnet test --verbosity normal

# Executar uma classe específica
dotnet test --filter ClassName=PaymentServiceTests

# Gerar relatório de cobertura
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover
```

### Visual Studio

1. Abra o **Test Explorer** (Test > Test Explorer ou Ctrl+E, T)
2. Clique em "Run All Tests" ou execute testes específicos
3. Visualize resultados em tempo real

### VS Code

1. Instale a extensão "C# Dev Kit"
2. Clique no ícone de teste na barra lateral
3. Execute os testes desejados

## Testes Criados

### PaymentServiceTests.cs

Testes do serviço de negócio `PaymentService`:

| Teste | Descrição |
|-------|-----------|
| `CreateTransactionAsync_WithValidData_ShouldCreateTransaction` | Verifica criação de transação com dados válidos |
| `CreateTransactionAsync_ShouldPublishStartProcessingMessage` | Valida publicação de mensagem de início |
| `GetByPurchaseIdAsync_WithValidPurchaseId_ShouldReturnTransaction` | Busca transação por ID de compra |
| `GetByPurchaseIdAsync_WithInvalidPurchaseId_ShouldReturnNull` | Retorna null para compra inválida |
| `QueryTransactionsAsync_FilterByUserId_ShouldReturnOnlyUserTransactions` | Filtra por usuário |
| `QueryTransactionsAsync_FilterByStatus_ShouldReturnOnlyMatchingTransactions` | Filtra por status |
| `QueryTransactionsAsync_FilterByDateRange_ShouldReturnOnlyTransactionsInRange` | Filtra por período |
| `UpdateStatusAsync_WithValidTransaction_ShouldUpdateStatus` | Atualiza status de transação |
| `UpdateStatusAsync_ShouldPublishStatusChangedMessage` | Valida publicação de status |

### PaymentsControllerTests.cs

Testes dos endpoints da API:

| Teste | Descrição |
|-------|-----------|
| `GetByPurchaseId_WithValidPurchaseId_ShouldReturnOkResult` | Retorna 200 para compra válida |
| `GetByPurchaseId_WithInvalidPurchaseId_ShouldReturnNotFound` | Retorna 404 para compra inválida |
| `CreatePayment_WithValidRequest_ShouldReturnCreatedResult` | Cria pagamento e retorna 201 |
| `QueryTransactions_WithFilters_ShouldCallServiceWithCorrectParameters` | Testa filtros de query |
| `UpdatePaymentStatus_WithValidRequest_ShouldUpdateAndReturnOk` | Atualiza status com sucesso |

## Padrões Utilizados

### AAA (Arrange-Act-Assert)

```csharp
[Fact]
public async Task TestMethod()
{
    // Arrange - Preparação dos dados
    var data = Setup();
    
    // Act - Execução do método testado
    var result = await Method(data);
    
    // Assert - Validação do resultado
    Assert.NotNull(result);
}
```

### Mocking com Moq

```csharp
var mockService = new Mock<IMessagePublisher>();
mockService
    .Setup(s => s.PublishAsync(It.IsAny<string>(), It.IsAny<MessageEnvelope>()))
    .Returns(Task.CompletedTask);

// Verificar chamada
mockService.Verify(s => s.PublishAsync(It.IsAny<string>(), It.IsAny<MessageEnvelope>()), Times.Once);
```

### DbContext In-Memory

```csharp
private PaymentsDbContext CreateInMemoryContext()
{
    var options = new DbContextOptionsBuilder<PaymentsDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;
    
    return new PaymentsDbContext(options);
}
```

## Adicionando Novos Testes

### Template para novo teste de serviço

```csharp
[Fact]
public async Task MethodName_Condition_ExpectedBehavior()
{
    // Arrange
    var db = CreateInMemoryContext();
    var mockPublisher = new Mock<IMessagePublisher>();
    var service = new PaymentService(db, mockPublisher.Object);

    // Act
    var result = await service.SomeMethodAsync();

    // Assert
    Assert.NotNull(result);
}
```

### Template para novo teste de controller

```csharp
[Fact]
public async Task MethodName_Condition_ExpectedResult()
{
    // Arrange
    var mockService = new Mock<IPaymentService>();
    mockService
        .Setup(s => s.MethodAsync(It.IsAny<string>()))
        .ReturnsAsync(expectedResult);

    var controller = new PaymentsController(mockService.Object);

    // Act
    var result = await controller.EndpointMethod();

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
}
```

## Cobertura de Testes

Recomendamos manter uma cobertura de testes de **pelo menos 80%** do código crítico de negócio.

Para visualizar a cobertura:

```bash
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover /p:Exclude="[FCG.Payments]FCG.Payments.Program"
```

## Integração Contínua

Adicione ao seu pipeline de CI/CD:

```yaml
# GitHub Actions exemplo
- name: Run tests
  run: dotnet test --verbosity normal --logger trx --results-directory "test-results"
  
- name: Upload test results
  uses: actions/upload-artifact@v2
  if: always()
  with:
    name: test-results
    path: test-results/
```

## Boas Práticas

✅ **Faça:**
- Um teste por comportamento
- Nomear testes descritivamente
- Manter testes isolados e independentes
- Usar mocks para dependências externas
- Testar casos de sucesso e erro

❌ **Evite:**
- Múltiplas assertions em um teste
- Testes interdependentes
- Testes lentos (refatore para In-Memory)
- Testes de implementação ao invés de comportamento

## Próximos Passos

1. **Adicione testes para repositories** (quando implementados)
2. **Implemente testes de integração** para cenários end-to-end
3. **Configure relatório de cobertura** na CI/CD
4. **Adicione testes de performance** para operações críticas
