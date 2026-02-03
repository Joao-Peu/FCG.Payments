using FCG.Payments.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace FCG.Payments.Shared;

public class PaymentsDbContext : DbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options) { }

    public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = null!;
    public DbSet<AuditEvent> AuditEvents { get; set; } = null!;

    public override int SaveChanges()
    {
        AddAuditEvents().GetAwaiter().GetResult();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await AddAuditEvents();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private Task AddAuditEvents()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is PaymentTransaction && (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            .ToList();

        foreach (var e in entries)
        {
            var entity = e.Entity as PaymentTransaction;
            var before = e.State == EntityState.Added ? null : System.Text.Json.JsonSerializer.Serialize(e.OriginalValues.ToObject());
            var after = e.State == EntityState.Deleted ? null : System.Text.Json.JsonSerializer.Serialize(e.CurrentValues.ToObject());

            AuditEvents.Add(new AuditEvent
            {
                Id = Guid.NewGuid(),
                AggregateType = nameof(PaymentTransaction),
                AggregateId = entity!.Id.ToString(),
                EventType = e.State.ToString(),
                BeforeJson = before,
                AfterJson = after,
                CreatedAtUtc = DateTime.UtcNow,
                CorrelationId = entity?.CorrelationId
            });
        }

        return Task.CompletedTask;
    }
}