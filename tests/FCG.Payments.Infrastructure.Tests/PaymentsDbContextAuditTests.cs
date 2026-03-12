using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Enums;
using FCG.Payments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FCG.Payments.Infrastructure.Tests;

public class PaymentsDbContextAuditTests
{
    private PaymentsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PaymentsDbContext(options);
    }

    [Fact]
    public async Task SaveChanges_OnAdd_ShouldCreateAuditEvent()
    {
        var db = CreateContext();
        var tx = PaymentTransaction.Create("PUR-001", "USER-001", 100m, "corr-1");
        db.PaymentTransactions.Add(tx);

        await db.SaveChangesAsync();

        var audits = await db.AuditEvents.ToListAsync();
        Assert.Single(audits);
        Assert.Equal("Added", audits[0].EventType);
        Assert.Equal(nameof(PaymentTransaction), audits[0].AggregateType);
        Assert.Null(audits[0].BeforeJson);
        Assert.NotNull(audits[0].AfterJson);
    }

    [Fact]
    public async Task SaveChanges_OnModify_ShouldCreateAuditEventWithBeforeAndAfter()
    {
        var db = CreateContext();
        var tx = PaymentTransaction.Create("PUR-001", "USER-001", 100m, "corr-1");
        db.PaymentTransactions.Add(tx);
        await db.SaveChangesAsync();

        tx.UpdateStatus(PaymentStatus.Paid);
        db.PaymentTransactions.Update(tx);
        await db.SaveChangesAsync();

        var audits = await db.AuditEvents.Where(a => a.EventType == "Modified").ToListAsync();
        Assert.Single(audits);
        Assert.NotNull(audits[0].BeforeJson);
        Assert.NotNull(audits[0].AfterJson);
    }
}
