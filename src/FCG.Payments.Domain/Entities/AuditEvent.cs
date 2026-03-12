namespace FCG.Payments.Domain.Entities;

public class AuditEvent
{
    public Guid Id { get; set; }
    public string AggregateType { get; set; } = null!;
    public string AggregateId { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? CorrelationId { get; set; }
    public string? TraceId { get; set; }
    public string? UserId { get; set; }
}
