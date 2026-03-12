using FCG.Payments.Application.Queries.QueryPaymentTransactions;
using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Enums;
using FCG.Payments.Domain.Interfaces;
using Moq;
using Xunit;

namespace FCG.Payments.Application.Tests;

public class QueryPaymentTransactionsHandlerTests
{
    private readonly Mock<IPaymentTransactionRepository> _mockRepo;
    private readonly QueryPaymentTransactionsHandler _handler;

    public QueryPaymentTransactionsHandlerTests()
    {
        _mockRepo = new Mock<IPaymentTransactionRepository>();
        _handler = new QueryPaymentTransactionsHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_WithFilters_ShouldPassFiltersToRepository()
    {
        var userId = "USER-001";
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;
        var status = PaymentStatus.Paid;

        var transactions = new List<PaymentTransaction>
        {
            PaymentTransaction.Create("PUR-001", userId, 100m, "corr-1")
        };

        _mockRepo.Setup(r => r.QueryAsync(userId, from, to, status, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        var query = new QueryPaymentTransactionsQuery(userId, from, to, status);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Single(result);
        _mockRepo.Verify(r => r.QueryAsync(userId, from, to, status, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoFilters_ShouldReturnAll()
    {
        var transactions = new List<PaymentTransaction>
        {
            PaymentTransaction.Create("PUR-001", "USER-001", 100m, "corr-1"),
            PaymentTransaction.Create("PUR-002", "USER-002", 200m, "corr-2")
        };

        _mockRepo.Setup(r => r.QueryAsync(null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        var query = new QueryPaymentTransactionsQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(2, result.Count());
    }
}
