using System.Text.Json;
using FCG.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCG.Payments.Infrastructure.Persistence;

public class PaymentsDbContext : DbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options) { }

    public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = null!;
    public DbSet<AuditEvent> AuditEvents { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentsDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        AddAuditEvents();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AddAuditEvents();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void AddAuditEvents()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is PaymentTransaction &&
                        (e.State == EntityState.Added ||
                         e.State == EntityState.Modified ||
                         e.State == EntityState.Deleted))
            .ToList();

        foreach (var e in entries)
        {
            var entity = (PaymentTransaction)e.Entity;
            var before = e.State == EntityState.Added
                ? null
                : JsonSerializer.Serialize(e.OriginalValues.ToObject());
            var after = e.State == EntityState.Deleted
                ? null
                : JsonSerializer.Serialize(e.CurrentValues.ToObject());

            AuditEvents.Add(new AuditEvent
            {
                Id = Guid.NewGuid(),
                AggregateType = nameof(PaymentTransaction),
                AggregateId = entity.Id.ToString(),
                EventType = e.State.ToString(),
                BeforeJson = before,
                AfterJson = after,
                CreatedAtUtc = DateTime.UtcNow,
                CorrelationId = entity.CorrelationId
            });
        }
    }
}
