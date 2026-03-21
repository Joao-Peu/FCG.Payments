using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Enums;
using FCG.Payments.Infrastructure.Persistence;
using FCG.Payments.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FCG.Payments.Infrastructure.Tests;

public class PaymentTransactionRepositoryTests
{
    private PaymentsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PaymentsDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistTransaction()
    {
        var db = CreateContext();
        var repo = new PaymentTransactionRepository(db);
        var tx = PaymentTransaction.Create("PUR-001", "USER-001", "GAME-001", 100m, PaymentStatus.Approved);

        await repo.AddAsync(tx);

        var found = await db.PaymentTransactions.FindAsync(tx.Id);
        Assert.NotNull(found);
        Assert.Equal("PUR-001", found.PurchaseId);
        Assert.Equal(PaymentStatus.Approved, found.Status);
    }

    [Fact]
    public async Task GetByPurchaseIdAsync_ShouldReturnTransaction()
    {
        var db = CreateContext();
        var repo = new PaymentTransactionRepository(db);
        var tx = PaymentTransaction.Create("PUR-001", "USER-001", "GAME-001", 100m, PaymentStatus.Approved);
        await repo.AddAsync(tx);

        var result = await repo.GetByPurchaseIdAsync("PUR-001");

        Assert.NotNull(result);
        Assert.Equal("PUR-001", result.PurchaseId);
    }

    [Fact]
    public async Task GetByPurchaseIdAsync_NonExistent_ShouldReturnNull()
    {
        var db = CreateContext();
        var repo = new PaymentTransactionRepository(db);

        var result = await repo.GetByPurchaseIdAsync("NONEXISTENT");

        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsPendingAsync_WithPending_ShouldReturnTrue()
    {
        var db = CreateContext();
        var repo = new PaymentTransactionRepository(db);
        await repo.AddAsync(PaymentTransaction.Create("PUR-001", "USER-001", "GAME-001", 100m, PaymentStatus.Pending));

        var result = await repo.ExistsPendingAsync("USER-001", "GAME-001");

        Assert.True(result);
    }

    [Fact]
    public async Task ExistsPendingAsync_WithApproved_ShouldReturnFalse()
    {
        var db = CreateContext();
        var repo = new PaymentTransactionRepository(db);
        await repo.AddAsync(PaymentTransaction.Create("PUR-001", "USER-001", "GAME-001", 100m, PaymentStatus.Approved));

        var result = await repo.ExistsPendingAsync("USER-001", "GAME-001");

        Assert.False(result);
    }

    [Fact]
    public async Task ExistsPendingAsync_DifferentGame_ShouldReturnFalse()
    {
        var db = CreateContext();
        var repo = new PaymentTransactionRepository(db);
        await repo.AddAsync(PaymentTransaction.Create("PUR-001", "USER-001", "GAME-001", 100m, PaymentStatus.Pending));

        var result = await repo.ExistsPendingAsync("USER-001", "GAME-002");

        Assert.False(result);
    }
}
