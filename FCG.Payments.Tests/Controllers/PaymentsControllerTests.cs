using Moq;
using Xunit;
using FCG.Payments.Controllers;
using FCG.Payments.Shared;
using FCG.Payments.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FCG.Payments.Tests.Controllers;

public class PaymentsControllerTests
{
    private Mock<IPaymentService> CreateMockPaymentService()
    {
        return new Mock<IPaymentService>();
    }

    [Fact]
    public async Task GetByPurchaseId_WithValidPurchaseId_ShouldReturnOkResult()
    {
        // Arrange
        var mockService = CreateMockPaymentService();
        var purchaseId = "PURCHASE-001";
        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PurchaseId = purchaseId,
            UserId = "USER-123",
            Amount = 100.50m,
            Status = PaymentStatus.Created,
            CreatedAtUtc = DateTime.UtcNow
        };

        mockService
            .Setup(s => s.GetByPurchaseIdAsync(purchaseId, It.IsAny<string?>()))
            .ReturnsAsync(transaction);

        var controller = new PaymentsController(mockService.Object);

        // Act
        var result = await controller.GetByPurchaseId(purchaseId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var returnedTransaction = Assert.IsType<PaymentTransaction>(okResult.Value);
        Assert.Equal(purchaseId, returnedTransaction.PurchaseId);
    }

    [Fact]
    public async Task GetByPurchaseId_WithInvalidPurchaseId_ShouldReturnNotFound()
    {
        // Arrange
        var mockService = CreateMockPaymentService();
        var purchaseId = "NONEXISTENT";

        mockService
            .Setup(s => s.GetByPurchaseIdAsync(purchaseId, It.IsAny<string?>()))
            .ReturnsAsync((PaymentTransaction?)null);

        var controller = new PaymentsController(mockService.Object);

        // Act
        var result = await controller.GetByPurchaseId(purchaseId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreatePayment_WithValidRequest_ShouldReturnCreatedResult()
    {
        // Arrange
        var mockService = CreateMockPaymentService();
        var request = new CreatePaymentRequest
        {
            PurchaseId = "PURCHASE-002",
            Amount = 250.75m
        };

        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PurchaseId = request.PurchaseId,
            UserId = "current-user",
            Amount = request.Amount,
            Status = PaymentStatus.Created,
            CreatedAtUtc = DateTime.UtcNow
        };

        mockService
            .Setup(s => s.CreateTransactionAsync(
                request.PurchaseId,
                It.IsAny<string>(),
                request.Amount,
                It.IsAny<string>()))
            .ReturnsAsync(transaction);

        var controller = new PaymentsController(mockService.Object);

        // Act
        var result = await controller.CreatePayment(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(PaymentsController.GetByPurchaseId), createdResult.ActionName);
        var returnedTransaction = Assert.IsType<PaymentTransaction>(createdResult.Value);
        Assert.Equal(request.PurchaseId, returnedTransaction.PurchaseId);
    }

    [Fact]
    public async Task QueryTransactions_WithFilters_ShouldCallServiceWithCorrectParameters()
    {
        // Arrange
        var mockService = CreateMockPaymentService();
        var userId = "USER-100";
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;
        var status = PaymentStatus.Paid;

        var transactions = new List<PaymentTransaction>
        {
            new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                PurchaseId = "PURCHASE-100",
                UserId = userId,
                Amount = 100m,
                Status = status,
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        mockService
            .Setup(s => s.QueryTransactionsAsync(userId, from, to, status))
            .ReturnsAsync(transactions);

        var controller = new PaymentsController(mockService.Object);

        // Act
        var result = await controller.QueryTransactions(userId, from, to, status);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedTransactions = Assert.IsAssignableFrom<IEnumerable<PaymentTransaction>>(okResult.Value);
        Assert.Single(returnedTransactions);
        mockService.Verify(s => s.QueryTransactionsAsync(userId, from, to, status), Times.Once);
    }

    [Fact]
    public async Task UpdatePaymentStatus_WithValidRequest_ShouldUpdateAndReturnOk()
    {
        // Arrange
        var mockService = CreateMockPaymentService();
        var transactionId = Guid.NewGuid();
        var newStatus = PaymentStatus.Processing;

        mockService
            .Setup(s => s.UpdateStatusAsync(transactionId, newStatus, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var controller = new PaymentsController(mockService.Object);

        // Act
        var result = await controller.UpdatePaymentStatus(transactionId, newStatus);

        // Assert
        Assert.IsType<OkResult>(result);
        mockService.Verify(s => s.UpdateStatusAsync(transactionId, newStatus, It.IsAny<string>()), Times.Once);
    }
}

// Test request model (if not already in the project)
public record CreatePaymentRequest
{
    public string PurchaseId { get; set; } = null!;
    public decimal Amount { get; set; }
}
