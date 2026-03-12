using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Enums;
using FCG.Payments.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCG.Payments.Infrastructure.Persistence.Repositories;

public class PaymentTransactionRepository : IPaymentTransactionRepository
{
    private readonly PaymentsDbContext _db;

    public PaymentTransactionRepository(PaymentsDbContext db)
    {
        _db = db;
    }

    public async Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.PaymentTransactions.FindAsync([id], cancellationToken);
    }

    public async Task<PaymentTransaction?> GetByPurchaseIdAsync(string purchaseId, CancellationToken cancellationToken = default)
    {
        return await _db.PaymentTransactions
            .FirstOrDefaultAsync(x => x.PurchaseId == purchaseId, cancellationToken);
    }

    public async Task<IEnumerable<PaymentTransaction>> QueryAsync(
        string? userId = null,
        DateTime? from = null,
        DateTime? to = null,
        PaymentStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.PaymentTransactions.AsQueryable();

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(x => x.UserId == userId);
        if (from.HasValue)
            query = query.Where(x => x.CreatedAtUtc >= from.Value);
        if (to.HasValue)
            query = query.Where(x => x.CreatedAtUtc <= to.Value);
        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default)
    {
        _db.PaymentTransactions.Add(transaction);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default)
    {
        _db.PaymentTransactions.Update(transaction);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
