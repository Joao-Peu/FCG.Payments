using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Enums;

namespace FCG.Payments.Domain.Interfaces;

public interface IPaymentTransactionRepository
{
    Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentTransaction?> GetByPurchaseIdAsync(string purchaseId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentTransaction>> QueryAsync(
        string? userId = null,
        DateTime? from = null,
        DateTime? to = null,
        PaymentStatus? status = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default);
    Task UpdateAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default);
}
