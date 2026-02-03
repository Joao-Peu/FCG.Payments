using FCG.Payments.Shared.Messaging;
using FCG.Payments.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace FCG.Payments.Shared;

public class PaymentService : IPaymentService
{
    private readonly PaymentsDbContext _db;
    private readonly IMessagePublisher _publisher;

    public PaymentService(PaymentsDbContext db, IMessagePublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task<PaymentTransaction> CreateTransactionAsync(string purchaseId, string userId, decimal amount, string correlationId)
    {
        var tx = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PurchaseId = purchaseId,
            UserId = userId,
            Amount = amount,
            Status = PaymentStatus.Created,
            CreatedAtUtc = DateTime.UtcNow,
            CorrelationId = correlationId
        };

        _db.PaymentTransactions.Add(tx);
        await _db.SaveChangesAsync();

        // publish internal message to start processing
        var envelope = new MessageEnvelope("StartPaymentProcessing", System.Text.Json.JsonSerializer.Serialize(new { tx.Id, tx.PurchaseId, tx.UserId, tx.Amount }), correlationId, "");
        await _publisher.PublishAsync("payments-start", envelope);

        return tx;
    }

    public async Task<PaymentTransaction?> GetByPurchaseIdAsync(string purchaseId, string? requesterUserId = null)
    {
        var q = _db.PaymentTransactions.AsQueryable();
        if (!string.IsNullOrEmpty(requesterUserId)) q = q.Where(x => x.UserId == requesterUserId || x.PurchaseId == purchaseId);
        return await q.FirstOrDefaultAsync(x => x.PurchaseId == purchaseId);
    }

    public async Task<IEnumerable<PaymentTransaction>> QueryTransactionsAsync(string? userId, DateTime? from, DateTime? to, PaymentStatus? status)
    {
        var q = _db.PaymentTransactions.AsQueryable();
        if (!string.IsNullOrEmpty(userId)) q = q.Where(x => x.UserId == userId);
        if (from.HasValue) q = q.Where(x => x.CreatedAtUtc >= from.Value);
        if (to.HasValue) q = q.Where(x => x.CreatedAtUtc <= to.Value);
        if (status.HasValue) q = q.Where(x => x.Status == status.Value);
        return await q.ToListAsync();
    }

    public async Task UpdateStatusAsync(Guid transactionId, PaymentStatus status, string? correlationId)
    {
        var tx = await _db.PaymentTransactions.FindAsync(transactionId);
        if (tx == null) return;
        tx.Status = status;
        tx.UpdatedAtUtc = DateTime.UtcNow;
        tx.CorrelationId = correlationId ?? tx.CorrelationId;
        await _db.SaveChangesAsync();

        // publish PaymentProcessed event
        var envelope = new MessageEnvelope("PaymentProcessed", System.Text.Json.JsonSerializer.Serialize(new { tx.PurchaseId, Status = tx.Status.ToString(), ProcessedAtUtc = DateTime.UtcNow, CorrelationId = tx.CorrelationId }), tx.CorrelationId ?? string.Empty, "");
        await _publisher.PublishAsync("payments-processed", envelope);

        // publish NotificationRequested
        var notify = new MessageEnvelope("NotificationRequested", System.Text.Json.JsonSerializer.Serialize(new { tx.UserId, tx.PurchaseId, Status = tx.Status.ToString(), CorrelationId = tx.CorrelationId }), tx.CorrelationId ?? string.Empty, "");
        await _publisher.PublishAsync("notifications", notify);
    }
}