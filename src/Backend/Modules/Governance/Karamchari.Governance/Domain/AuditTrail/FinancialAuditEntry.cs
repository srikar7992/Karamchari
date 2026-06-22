namespace Karamchari.Governance.Domain.AuditTrail;

public sealed class FinancialAuditEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public AuditOperation Operation { get; init; }
    public Guid ActorId { get; init; }
    public string ActorName { get; init; } = string.Empty;
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public string? BeforeJson { get; init; }
    public string? AfterJson { get; init; }
    public string? CorrelationId { get; init; }
}
