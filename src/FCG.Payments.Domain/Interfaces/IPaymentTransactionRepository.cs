using FCG.Payments.Domain.Entities;

namespace FCG.Payments.Domain.Interfaces;

public interface IPaymentTransactionRepository
{
    Task<PaymentTransaction?> GetByPurchaseIdAsync(string purchaseId, CancellationToken cancellationToken = default);
    Task<bool> ExistsPendingAsync(string userId, string gameId, CancellationToken cancellationToken = default);
    Task AddAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default);
}
