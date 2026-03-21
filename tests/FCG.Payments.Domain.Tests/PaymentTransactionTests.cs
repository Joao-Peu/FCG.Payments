using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Enums;
using Xunit;

namespace FCG.Payments.Domain.Tests;

public class PaymentTransactionTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateTransaction()
    {
        var tx = PaymentTransaction.Create("PUR-001", "USER-001", "GAME-001", 100.50m, PaymentStatus.Approved);

        Assert.NotEqual(Guid.Empty, tx.Id);
        Assert.Equal("PUR-001", tx.PurchaseId);
        Assert.Equal("USER-001", tx.UserId);
        Assert.Equal("GAME-001", tx.GameId);
        Assert.Equal(100.50m, tx.Amount);
        Assert.Equal(PaymentStatus.Approved, tx.Status);
    }

    [Fact]
    public void Create_WithRejectedStatus_ShouldCreate()
    {
        var tx = PaymentTransaction.Create("PUR-001", "USER-001", "GAME-001", 50m, PaymentStatus.Rejected);

        Assert.Equal(PaymentStatus.Rejected, tx.Status);
    }

    [Fact]
    public void Create_WithPendingStatus_ShouldCreate()
    {
        var tx = PaymentTransaction.Create("PUR-001", "USER-001", "GAME-001", 50m, PaymentStatus.Pending);

        Assert.Equal(PaymentStatus.Pending, tx.Status);
    }

    [Theory]
    [InlineData("", "user", "game", 10)]
    [InlineData("  ", "user", "game", 10)]
    public void Create_WithEmptyPurchaseId_ShouldThrow(string purchaseId, string userId, string gameId, decimal amount)
    {
        Assert.Throws<ArgumentException>(() =>
            PaymentTransaction.Create(purchaseId, userId, gameId, amount, PaymentStatus.Approved));
    }

    [Theory]
    [InlineData("pur", "", "game", 10)]
    [InlineData("pur", "  ", "game", 10)]
    public void Create_WithEmptyUserId_ShouldThrow(string purchaseId, string userId, string gameId, decimal amount)
    {
        Assert.Throws<ArgumentException>(() =>
            PaymentTransaction.Create(purchaseId, userId, gameId, amount, PaymentStatus.Approved));
    }

    [Theory]
    [InlineData("pur", "user", "", 10)]
    [InlineData("pur", "user", "  ", 10)]
    public void Create_WithEmptyGameId_ShouldThrow(string purchaseId, string userId, string gameId, decimal amount)
    {
        Assert.Throws<ArgumentException>(() =>
            PaymentTransaction.Create(purchaseId, userId, gameId, amount, PaymentStatus.Approved));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void Create_WithNonPositiveAmount_ShouldThrow(decimal amount)
    {
        Assert.Throws<ArgumentException>(() =>
            PaymentTransaction.Create("PUR-001", "USER-001", "GAME-001", amount, PaymentStatus.Approved));
    }
}
