using FCG.Payments.Application.Commands.ProcessPayment;
using FCG.Payments.Application.Messaging;
using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Enums;
using FCG.Payments.Domain.Interfaces;
using Moq;
using Xunit;

namespace FCG.Payments.Application.Tests;

public class ProcessPaymentHandlerTests
{
    private readonly Mock<IPaymentTransactionRepository> _mockRepo;
    private readonly Mock<IMessagePublisher> _mockPublisher;
    private readonly ProcessPaymentHandler _handler;

    public ProcessPaymentHandlerTests()
    {
        _mockRepo = new Mock<IPaymentTransactionRepository>();
        _mockPublisher = new Mock<IMessagePublisher>();
        _handler = new ProcessPaymentHandler(_mockRepo.Object, _mockPublisher.Object);
    }

    [Fact]
    public async Task Handle_EvenCents_ShouldSetStatusPaid()
    {
        var tx = PaymentTransaction.Create("PUR-001", "USER-001", 100.50m, "corr-1", "GAME-001");
        _mockRepo.Setup(r => r.GetByIdAsync(tx.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx);

        var command = new ProcessPaymentCommand(tx.Id, 100.50m, "corr-1");
        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(PaymentStatus.Paid, tx.Status);
        _mockRepo.Verify(r => r.UpdateAsync(tx, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OddCents_ShouldSetStatusFailed()
    {
        var tx = PaymentTransaction.Create("PUR-001", "USER-001", 100.51m, "corr-1", "GAME-001");
        _mockRepo.Setup(r => r.GetByIdAsync(tx.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx);

        var command = new ProcessPaymentCommand(tx.Id, 100.51m, "corr-1");
        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(PaymentStatus.Failed, tx.Status);
    }

    [Fact]
    public async Task Handle_ShouldPublishPaymentProcessedEvent()
    {
        var tx = PaymentTransaction.Create("PUR-001", "USER-001", 100.50m, "corr-1", "GAME-001");
        _mockRepo.Setup(r => r.GetByIdAsync(tx.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx);

        var command = new ProcessPaymentCommand(tx.Id, 100.50m, "corr-1");
        await _handler.Handle(command, CancellationToken.None);

        _mockPublisher.Verify(p => p.PublishAsync(
            "payments-processed",
            It.Is<MessageEnvelope>(e => e.MessageType == "PaymentProcessed"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPublishNotification()
    {
        var tx = PaymentTransaction.Create("PUR-001", "USER-001", 100.50m, "corr-1", "GAME-001");
        _mockRepo.Setup(r => r.GetByIdAsync(tx.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx);

        var command = new ProcessPaymentCommand(tx.Id, 100.50m, "corr-1");
        await _handler.Handle(command, CancellationToken.None);

        _mockPublisher.Verify(p => p.PublishAsync(
            "notifications",
            It.Is<MessageEnvelope>(e => e.MessageType == "NotificationRequested"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TransactionNotFound_ShouldReturnWithoutError()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentTransaction?)null);

        var command = new ProcessPaymentCommand(Guid.NewGuid(), 100m, "corr-1");
        await _handler.Handle(command, CancellationToken.None);

        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<PaymentTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockPublisher.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<MessageEnvelope>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
