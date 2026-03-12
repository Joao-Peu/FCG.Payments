using FCG.Payments.Application.Commands.CreatePaymentTransaction;
using FCG.Payments.Application.Messaging;
using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Enums;
using FCG.Payments.Domain.Interfaces;
using Moq;
using Xunit;

namespace FCG.Payments.Application.Tests;

public class CreatePaymentTransactionHandlerTests
{
    private readonly Mock<IPaymentTransactionRepository> _mockRepo;
    private readonly Mock<IMessagePublisher> _mockPublisher;
    private readonly CreatePaymentTransactionHandler _handler;

    public CreatePaymentTransactionHandlerTests()
    {
        _mockRepo = new Mock<IPaymentTransactionRepository>();
        _mockPublisher = new Mock<IMessagePublisher>();
        _handler = new CreatePaymentTransactionHandler(_mockRepo.Object, _mockPublisher.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldCreateAndReturnResult()
    {
        var command = new CreatePaymentTransactionCommand(
            "PUR-001", "USER-001", "GAME-001", 100.50m, "corr-1");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("PUR-001", result.PurchaseId);
        Assert.Equal("USER-001", result.UserId);
        Assert.Equal("GAME-001", result.GameId);
        Assert.Equal(100.50m, result.Amount);
        Assert.Equal(PaymentStatus.Created.ToString(), result.Status);
    }

    [Fact]
    public async Task Handle_ShouldPersistTransaction()
    {
        var command = new CreatePaymentTransactionCommand(
            "PUR-001", "USER-001", "GAME-001", 100m, "corr-1");

        await _handler.Handle(command, CancellationToken.None);

        _mockRepo.Verify(r => r.AddAsync(
            It.Is<PaymentTransaction>(t =>
                t.PurchaseId == "PUR-001" &&
                t.UserId == "USER-001" &&
                t.Amount == 100m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPublishToPaymentsStart()
    {
        var command = new CreatePaymentTransactionCommand(
            "PUR-001", "USER-001", "GAME-001", 50.25m, "corr-1");

        await _handler.Handle(command, CancellationToken.None);

        _mockPublisher.Verify(p => p.PublishAsync(
            "payments-start",
            It.Is<MessageEnvelope>(e => e.MessageType == "StartPaymentProcessing"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
