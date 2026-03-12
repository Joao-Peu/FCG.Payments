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
        var tx = PaymentTransaction.Create("PUR-001", "USER-001", 100m, "corr-1", "GAME-001");

        await repo.AddAsync(tx);

        var found = await db.PaymentTransactions.FindAsync(tx.Id);
        Assert.NotNull(found);
        Assert.Equal("PUR-001", found.PurchaseId);
        Assert.Equal("GAME-001", found.GameId);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnTransaction()
    {
        var db = CreateContext();
        var repo = new PaymentTransactionRepository(db);
        var tx = PaymentTransaction.Create("PUR-001", "USER-001", 100m, "corr-1");
        await repo.AddAsync(tx);

        var result = await repo.GetByIdAsync(tx.Id);

        Assert.NotNull(result);
        Assert.Equal(tx.Id, result.Id);
    }

    [Fact]
    public async Task GetByPurchaseIdAsync_ShouldReturnTransaction()
    {
        var db = CreateContext();
        var repo = new PaymentTransactionRepository(db);
        var tx = PaymentTransaction.Create("PUR-001", "USER-001", 100m, "corr-1");
        await repo.AddAsync(tx);

        var result = await repo.GetByPurchaseIdAsync("PUR-001");

        Assert.NotNull(result);
        Assert.Equal("PUR-001", result.PurchaseId);
    }

    [Fact]
    public async Task QueryAsync_FilterByUserId_ShouldReturnOnlyUserTransactions()
    {
        var db = CreateContext();
        var repo = new PaymentTransactionRepository(db);

        await repo.AddAsync(PaymentTransaction.Create("PUR-001", "USER-001", 100m, "corr-1"));
        await repo.AddAsync(PaymentTransaction.Create("PUR-002", "USER-001", 200m, "corr-2"));
        await repo.AddAsync(PaymentTransaction.Create("PUR-003", "USER-002", 150m, "corr-3"));

        var result = await repo.QueryAsync(userId: "USER-001");

        Assert.Equal(2, result.Count());
        Assert.All(result, t => Assert.Equal("USER-001", t.UserId));
    }

    [Fact]
    public async Task QueryAsync_FilterByStatus_ShouldReturnOnlyMatchingTransactions()
    {
        var db = CreateContext();
        var repo = new PaymentTransactionRepository(db);

        var tx1 = PaymentTransaction.Create("PUR-001", "USER-001", 100m, "corr-1");
        var tx2 = PaymentTransaction.Create("PUR-002", "USER-001", 200m, "corr-2");
        tx2.UpdateStatus(PaymentStatus.Paid);

        await repo.AddAsync(tx1);
        await repo.AddAsync(tx2);

        var result = await repo.QueryAsync(status: PaymentStatus.Paid);

        Assert.Single(result);
        Assert.Equal(PaymentStatus.Paid, result.First().Status);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistChanges()
    {
        var db = CreateContext();
        var repo = new PaymentTransactionRepository(db);
        var tx = PaymentTransaction.Create("PUR-001", "USER-001", 100m, "corr-1");
        await repo.AddAsync(tx);

        tx.UpdateStatus(PaymentStatus.Paid, "new-corr");
        await repo.UpdateAsync(tx);

        var updated = await repo.GetByIdAsync(tx.Id);
        Assert.Equal(PaymentStatus.Paid, updated!.Status);
    }
}
