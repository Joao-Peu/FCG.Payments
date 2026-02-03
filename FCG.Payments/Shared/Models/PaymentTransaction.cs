using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FCG.Payments.Shared.Models;

public enum PaymentStatus
{
    Created,
    Processing,
    Paid,
    Failed,
    Cancelled
}

public class PaymentTransaction
{
    [Key]
    public Guid Id { get; set; }

    public string PurchaseId { get; set; } = null!; // external purchase identifier

    public string UserId { get; set; } = null!;

    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string? CorrelationId { get; set; }
}