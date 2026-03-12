using FCG.Payments.Application.Queries.GetPaymentByPurchaseId;
using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Interfaces;
using Moq;
using Xunit;

namespace FCG.Payments.Application.Tests;

public class GetPaymentByPurchaseIdHandlerTests
{
    private readonly Mock<IPaymentTransactionRepository> _mockRepo;
    private readonly GetPaymentByPurchaseIdHandler _handler;

    public GetPaymentByPurchaseIdHandlerTests()
    {
        _mockRepo = new Mock<IPaymentTransactionRepository>();
        _handler = new GetPaymentByPurchaseIdHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ExistingPurchaseId_ShouldReturnTransaction()
    {
        var tx = PaymentTransaction.Create("PUR-001", "USER-001", 100m, "corr-1");
        _mockRepo.Setup(r => r.GetByPurchaseIdAsync("PUR-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx);

        var query = new GetPaymentByPurchaseIdQuery("PUR-001");
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("PUR-001", result.PurchaseId);
    }

    [Fact]
    public async Task Handle_NonExistentPurchaseId_ShouldReturnNull()
    {
        _mockRepo.Setup(r => r.GetByPurchaseIdAsync("NONEXISTENT", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentTransaction?)null);

        var query = new GetPaymentByPurchaseIdQuery("NONEXISTENT");
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Null(result);
    }
}
