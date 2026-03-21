using FCG.Payments.Domain.Enums;

namespace FCG.Payments.Domain.Entities;

public class PaymentTransaction
{
    public Guid Id { get; private set; }
    public string PurchaseId { get; private set; } = null!;
    public string UserId { get; private set; } = null!;
    public string GameId { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private PaymentTransaction() { }

    public static PaymentTransaction Create(
        string purchaseId,
        string userId,
        string gameId,
        decimal amount,
        PaymentStatus status)
    {
        if (string.IsNullOrWhiteSpace(purchaseId))
            throw new ArgumentException("PurchaseId is required.", nameof(purchaseId));
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(gameId))
            throw new ArgumentException("GameId is required.", nameof(gameId));
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive.", nameof(amount));

        return new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PurchaseId = purchaseId,
            UserId = userId,
            GameId = gameId,
            Amount = amount,
            Status = status,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
