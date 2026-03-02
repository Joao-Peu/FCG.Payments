using Moq;
using Xunit;
using FCG.Payments.Shared;
using FCG.Payments.Shared.Messaging;
using FCG.Payments.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace FCG.Payments.Tests.Services;

public class PaymentServiceTests
{
    private PaymentsDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PaymentsDbContext(options);
    }

    [Fact]
    public async Task CreateTransactionAsync_WithValidData_ShouldCreateTransaction()
    {
        // Arrange
        var db = CreateInMemoryContext();
        var mockPublisher = new Mock<IMessagePublisher>();
        var service = new PaymentService(db, mockPublisher.Object);

        var purchaseId = "PURCHASE-001";
        var userId = "USER-123";
        var amount = 100.50m;
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var result = await service.CreateTransactionAsync(purchaseId, userId, amount, correlationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(purchaseId, result.PurchaseId);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(amount, result.Amount);
        Assert.Equal(PaymentStatus.Created, result.Status);
        Assert.Equal(correlationId, result.CorrelationId);
    }

    [Fact]
    public async Task CreateTransactionAsync_ShouldPublishStartProcessingMessage()
    {
        // Arrange
        var db = CreateInMemoryContext();
        var mockPublisher = new Mock<IMessagePublisher>();
        var service = new PaymentService(db, mockPublisher.Object);

        var purchaseId = "PURCHASE-002";
        var userId = "USER-124";
        var amount = 50.25m;
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var result = await service.CreateTransactionAsync(purchaseId, userId, amount, correlationId);

        // Assert
        mockPublisher.Verify(
            p => p.PublishAsync(
                "payments-start",
                It.IsAny<MessageEnvelope>()),
            Times.Once);
    }

    [Fact]
    public async Task GetByPurchaseIdAsync_WithValidPurchaseId_ShouldReturnTransaction()
    {
        // Arrange
        var db = CreateInMemoryContext();
        var mockPublisher = new Mock<IMessagePublisher>();
        var service = new PaymentService(db, mockPublisher.Object);

        var purchaseId = "PURCHASE-003";
        var userId = "USER-125";
        var transaction = await service.CreateTransactionAsync(purchaseId, userId, 75.00m, Guid.NewGuid().ToString());

        // Act
        var result = await service.GetByPurchaseIdAsync(purchaseId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(transaction.Id, result.Id);
        Assert.Equal(purchaseId, result.PurchaseId);
    }

    [Fact]
    public async Task GetByPurchaseIdAsync_WithInvalidPurchaseId_ShouldReturnNull()
    {
        // Arrange
        var db = CreateInMemoryContext();
        var mockPublisher = new Mock<IMessagePublisher>();
        var service = new PaymentService(db, mockPublisher.Object);

        // Act
        var result = await service.GetByPurchaseIdAsync("NONEXISTENT");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task QueryTransactionsAsync_FilterByUserId_ShouldReturnOnlyUserTransactions()
    {
        // Arrange
        var db = CreateInMemoryContext();
        var mockPublisher = new Mock<IMessagePublisher>();
        var service = new PaymentService(db, mockPublisher.Object);

        var userId1 = "USER-200";
        var userId2 = "USER-201";

        await service.CreateTransactionAsync("PURCHASE-100", userId1, 100m, Guid.NewGuid().ToString());
        await service.CreateTransactionAsync("PURCHASE-101", userId1, 200m, Guid.NewGuid().ToString());
        await service.CreateTransactionAsync("PURCHASE-102", userId2, 150m, Guid.NewGuid().ToString());

        // Act
        var result = await service.QueryTransactionsAsync(userId1, null, null, null);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, t => Assert.Equal(userId1, t.UserId));
    }

    [Fact]
    public async Task QueryTransactionsAsync_FilterByStatus_ShouldReturnOnlyMatchingTransactions()
    {
        // Arrange
        var db = CreateInMemoryContext();
        var mockPublisher = new Mock<IMessagePublisher>();
        var service = new PaymentService(db, mockPublisher.Object);

        await service.CreateTransactionAsync("PURCHASE-200", "USER-300", 100m, Guid.NewGuid().ToString());
        var tx2 = await service.CreateTransactionAsync("PURCHASE-201", "USER-301", 200m, Guid.NewGuid().ToString());
        
        // Update second transaction to Paid
        await service.UpdateStatusAsync(tx2.Id, PaymentStatus.Paid, Guid.NewGuid().ToString());

        // Act
        var result = await service.QueryTransactionsAsync(null, null, null, PaymentStatus.Paid);

        // Assert
        Assert.Single(result);
        Assert.Equal(PaymentStatus.Paid, result.First().Status);
    }

    [Fact]
    public async Task QueryTransactionsAsync_FilterByDateRange_ShouldReturnOnlyTransactionsInRange()
    {
        // Arrange
        var db = CreateInMemoryContext();
        var mockPublisher = new Mock<IMessagePublisher>();
        var service = new PaymentService(db, mockPublisher.Object);

        var now = DateTime.UtcNow;
        var before = now.AddHours(-1);
        var after = now.AddHours(1);

        await service.CreateTransactionAsync("PURCHASE-300", "USER-400", 100m, Guid.NewGuid().ToString());

        // Act
        var resultInRange = await service.QueryTransactionsAsync(null, before, after, null);
        var resultOutOfRange = await service.QueryTransactionsAsync(null, after, after.AddHours(1), null);

        // Assert
        Assert.Single(resultInRange);
        Assert.Empty(resultOutOfRange);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithValidTransaction_ShouldUpdateStatus()
    {
        // Arrange
        var db = CreateInMemoryContext();
        var mockPublisher = new Mock<IMessagePublisher>();
        var service = new PaymentService(db, mockPublisher.Object);

        var transaction = await service.CreateTransactionAsync("PURCHASE-400", "USER-500", 100m, Guid.NewGuid().ToString());

        // Act
        await service.UpdateStatusAsync(transaction.Id, PaymentStatus.Processing, Guid.NewGuid().ToString());

        // Assert
        var updated = await service.GetByPurchaseIdAsync("PURCHASE-400");
        Assert.NotNull(updated);
        Assert.Equal(PaymentStatus.Processing, updated.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldPublishStatusChangedMessage()
    {
        // Arrange
        var db = CreateInMemoryContext();
        var mockPublisher = new Mock<IMessagePublisher>();
        var service = new PaymentService(db, mockPublisher.Object);

        var transaction = await service.CreateTransactionAsync("PURCHASE-500", "USER-600", 100m, Guid.NewGuid().ToString());
        var correlationId = Guid.NewGuid().ToString();

        // Act
        await service.UpdateStatusAsync(transaction.Id, PaymentStatus.Paid, correlationId);

        // Assert
        mockPublisher.Verify(
            p => p.PublishAsync(
                "payments-status-changed",
                It.IsAny<MessageEnvelope>()),
            Times.Once);
    }
}
