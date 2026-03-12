using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Enums;
using Xunit;

namespace FCG.Payments.Domain.Tests;

public class PaymentTransactionTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateTransaction()
    {
        var tx = PaymentTransaction.Create("PUR-001", "USER-001", 100.50m, "corr-1", "GAME-001");

        Assert.NotEqual(Guid.Empty, tx.Id);
        Assert.Equal("PUR-001", tx.PurchaseId);
        Assert.Equal("USER-001", tx.UserId);
        Assert.Equal("GAME-001", tx.GameId);
        Assert.Equal(100.50m, tx.Amount);
        Assert.Equal(PaymentStatus.Created, tx.Status);
        Assert.Equal("corr-1", tx.CorrelationId);
    }

    [Fact]
    public void Create_WithNullGameId_ShouldCreateTransaction()
    {
        var tx = PaymentTransaction.Create("PUR-001", "USER-001", 50m, "corr-1");

        Assert.Null(tx.GameId);
        Assert.Equal(PaymentStatus.Created, tx.Status);
    }

    [Theory]
    [InlineData("", "user", 10, "corr")]
    [InlineData("  ", "user", 10, "corr")]
    [InlineData(null, "user", 10, "corr")]
    public void Create_WithEmptyPurchaseId_ShouldThrow(string? purchaseId, string userId, decimal amount, string correlationId)
    {
        Assert.Throws<ArgumentException>(() =>
            PaymentTransaction.Create(purchaseId!, userId, amount, correlationId));
    }

    [Theory]
    [InlineData("pur", "", 10, "corr")]
    [InlineData("pur", "  ", 10, "corr")]
    [InlineData("pur", null, 10, "corr")]
    public void Create_WithEmptyUserId_ShouldThrow(string purchaseId, string? userId, decimal amount, string correlationId)
    {
        Assert.Throws<ArgumentException>(() =>
            PaymentTransaction.Create(purchaseId, userId!, amount, correlationId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void Create_WithNonPositiveAmount_ShouldThrow(decimal amount)
    {
        Assert.Throws<ArgumentException>(() =>
            PaymentTransaction.Create("PUR-001", "USER-001", amount, "corr-1"));
    }

    [Theory]
    [InlineData(PaymentStatus.Created, PaymentStatus.Paid)]
    [InlineData(PaymentStatus.Created, PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Created, PaymentStatus.Processing)]
    [InlineData(PaymentStatus.Created, PaymentStatus.Cancelled)]
    [InlineData(PaymentStatus.Processing, PaymentStatus.Paid)]
    [InlineData(PaymentStatus.Processing, PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Processing, PaymentStatus.Cancelled)]
    public void UpdateStatus_ValidTransition_ShouldSucceed(PaymentStatus from, PaymentStatus to)
    {
        var tx = PaymentTransaction.Create("PUR-001", "USER-001", 100m, "corr-1");
        if (from == PaymentStatus.Processing)
            tx.UpdateStatus(PaymentStatus.Processing);

        tx.UpdateStatus(to, "new-corr");

        Assert.Equal(to, tx.Status);
        Assert.NotNull(tx.UpdatedAtUtc);
        Assert.Equal("new-corr", tx.CorrelationId);
    }

    [Theory]
    [InlineData(PaymentStatus.Paid, PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Paid, PaymentStatus.Created)]
    [InlineData(PaymentStatus.Failed, PaymentStatus.Paid)]
    [InlineData(PaymentStatus.Failed, PaymentStatus.Created)]
    [InlineData(PaymentStatus.Cancelled, PaymentStatus.Created)]
    public void UpdateStatus_InvalidTransition_ShouldThrow(PaymentStatus from, PaymentStatus to)
    {
        var tx = PaymentTransaction.Create("PUR-001", "USER-001", 100m, "corr-1");
        // Transition to the 'from' state first
        if (from != PaymentStatus.Created)
            tx.UpdateStatus(from);

        Assert.Throws<InvalidOperationException>(() => tx.UpdateStatus(to));
    }

    [Fact]
    public void UpdateStatus_WithNullCorrelationId_ShouldKeepExisting()
    {
        var tx = PaymentTransaction.Create("PUR-001", "USER-001", 100m, "original-corr");

        tx.UpdateStatus(PaymentStatus.Paid);

        Assert.Equal("original-corr", tx.CorrelationId);
    }
}
