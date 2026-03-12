using FCG.Payments.Domain.Enums;

namespace FCG.Payments.Domain.Entities;

public class PaymentTransaction
{
    public Guid Id { get; private set; }
    public string PurchaseId { get; private set; } = null!;
    public string UserId { get; private set; } = null!;
    public string? GameId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public string? CorrelationId { get; private set; }

    private PaymentTransaction() { }

    public static PaymentTransaction Create(
        string purchaseId,
        string userId,
        decimal amount,
        string correlationId,
        string? gameId = null)
    {
        if (string.IsNullOrWhiteSpace(purchaseId))
            throw new ArgumentException("PurchaseId is required.", nameof(purchaseId));
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive.", nameof(amount));

        return new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PurchaseId = purchaseId,
            UserId = userId,
            GameId = gameId,
            Amount = amount,
            Status = PaymentStatus.Created,
            CreatedAtUtc = DateTime.UtcNow,
            CorrelationId = correlationId
        };
    }

    public void UpdateStatus(PaymentStatus newStatus, string? correlationId = null)
    {
        ValidateStatusTransition(newStatus);
        Status = newStatus;
        UpdatedAtUtc = DateTime.UtcNow;
        if (correlationId is not null)
            CorrelationId = correlationId;
    }

    private void ValidateStatusTransition(PaymentStatus newStatus)
    {
        var valid = (Status, newStatus) switch
        {
            (PaymentStatus.Created, PaymentStatus.Processing) => true,
            (PaymentStatus.Created, PaymentStatus.Paid) => true,
            (PaymentStatus.Created, PaymentStatus.Failed) => true,
            (PaymentStatus.Created, PaymentStatus.Cancelled) => true,
            (PaymentStatus.Processing, PaymentStatus.Paid) => true,
            (PaymentStatus.Processing, PaymentStatus.Failed) => true,
            (PaymentStatus.Processing, PaymentStatus.Cancelled) => true,
            _ => false
        };

        if (!valid)
            throw new InvalidOperationException(
                $"Invalid status transition from {Status} to {newStatus}.");
    }
}
