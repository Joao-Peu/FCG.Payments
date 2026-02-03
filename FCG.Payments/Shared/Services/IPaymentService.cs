using FCG.Payments.Shared.Models;

namespace FCG.Payments.Shared;

public interface IPaymentService
{
    Task<PaymentTransaction?> GetByPurchaseIdAsync(string purchaseId, string? requesterUserId = null);
    Task<IEnumerable<PaymentTransaction>> QueryTransactionsAsync(string? userId, DateTime? from, DateTime? to, PaymentStatus? status);
    Task<PaymentTransaction> CreateTransactionAsync(string purchaseId, string userId, decimal amount, string correlationId);
    Task UpdateStatusAsync(Guid transactionId, PaymentStatus status, string? correlationId);
}