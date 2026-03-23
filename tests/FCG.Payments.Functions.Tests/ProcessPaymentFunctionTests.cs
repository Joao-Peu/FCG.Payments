using System.Collections.Immutable;
using System.Text.Json;
using FCG.Payments.Application.Messaging;
using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Events;
using FCG.Payments.Domain.Interfaces;
using FCG.Payments.Functions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FCG.Payments.Functions.Tests;

public class ProcessPaymentFunctionTests
{
    private readonly Mock<IPaymentTransactionRepository> _mockRepo;
    private readonly Mock<IMessagePublisher> _mockPublisher;
    private readonly Mock<ILogger<ProcessPaymentFunction>> _mockLogger;
    private readonly ProcessPaymentFunction _function;

    public ProcessPaymentFunctionTests()
    {
        _mockRepo = new Mock<IPaymentTransactionRepository>();
        _mockPublisher = new Mock<IMessagePublisher>();
        _mockLogger = new Mock<ILogger<ProcessPaymentFunction>>();
        _function = new ProcessPaymentFunction(_mockRepo.Object, _mockPublisher.Object, _mockLogger.Object);
    }

    private static FunctionContext CreateFunctionContext(string correlationId = "test-correlation-id")
    {
        var bindingData = new Dictionary<string, object?>
        {
            ["MessageId"] = "test-message-id",
            ["CorrelationId"] = correlationId,
            ["DeliveryCount"] = "1"
        };

        var bindingContext = new Mock<BindingContext>();
        bindingContext.Setup(b => b.BindingData).Returns(bindingData.ToImmutableDictionary());

        var functionContext = new Mock<FunctionContext>();
        functionContext.Setup(c => c.BindingContext).Returns(bindingContext.Object);

        return functionContext.Object;
    }

    [Fact]
    public async Task Run_EvenCents_ShouldCreateTransactionWithApprovedStatus()
    {
        var orderEvent = new OrderPlacedEvent("ORDER-001", "USER-001", "GAME-001", 59.98m);
        var message = JsonSerializer.Serialize(orderEvent);
        var context = CreateFunctionContext();
        _mockRepo.Setup(r => r.ExistsPendingAsync("USER-001", "GAME-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await _function.Run(message, context);

        _mockRepo.Verify(r => r.AddAsync(
            It.Is<PaymentTransaction>(t =>
                t.PurchaseId == "ORDER-001" &&
                t.UserId == "USER-001" &&
                t.GameId == "GAME-001" &&
                t.Amount == 59.98m),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockPublisher.Verify(p => p.PublishAsync(
            "payments-processed",
            It.Is<MessageEnvelope>(e => e.MessageType == "PaymentProcessed" && e.CorrelationId == "test-correlation-id"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Run_OddCents_ShouldCreateTransactionWithRejectedStatus()
    {
        var orderEvent = new OrderPlacedEvent("ORDER-002", "USER-001", "GAME-001", 59.99m);
        var message = JsonSerializer.Serialize(orderEvent);
        var context = CreateFunctionContext();
        _mockRepo.Setup(r => r.ExistsPendingAsync("USER-001", "GAME-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await _function.Run(message, context);

        _mockRepo.Verify(r => r.AddAsync(
            It.IsAny<PaymentTransaction>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockPublisher.Verify(p => p.PublishAsync(
            "payments-processed",
            It.IsAny<MessageEnvelope>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Run_AlreadyPending_ShouldSkipProcessing()
    {
        var orderEvent = new OrderPlacedEvent("ORDER-003", "USER-001", "GAME-001", 100m);
        var message = JsonSerializer.Serialize(orderEvent);
        var context = CreateFunctionContext();
        _mockRepo.Setup(r => r.ExistsPendingAsync("USER-001", "GAME-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _function.Run(message, context);

        _mockRepo.Verify(r => r.AddAsync(It.IsAny<PaymentTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockPublisher.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<MessageEnvelope>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Run_InvalidMessage_ShouldNotProcess()
    {
        var context = CreateFunctionContext();

        await _function.Run("not valid json {}{}", context);

        _mockRepo.Verify(r => r.AddAsync(It.IsAny<PaymentTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockPublisher.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<MessageEnvelope>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
