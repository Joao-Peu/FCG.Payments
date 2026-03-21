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

    public async Task<PaymentTransaction?> GetByPurchaseIdAsync(string purchaseId, CancellationToken cancellationToken = default)
    {
        return await _db.PaymentTransactions
            .FirstOrDefaultAsync(x => x.PurchaseId == purchaseId, cancellationToken);
    }

    public async Task<bool> ExistsPendingAsync(string userId, string gameId, CancellationToken cancellationToken = default)
    {
        return await _db.PaymentTransactions
            .AnyAsync(x =>
                x.UserId == userId &&
                x.GameId == gameId &&
                x.Status == PaymentStatus.Pending,
                cancellationToken);
    }

    public async Task AddAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default)
    {
        _db.PaymentTransactions.Add(transaction);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
