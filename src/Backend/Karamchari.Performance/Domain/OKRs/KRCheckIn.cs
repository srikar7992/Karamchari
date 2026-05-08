using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.OKRs;

public sealed class KRCheckIn : Entity<Guid>
{
    private KRCheckIn() { /* EF materialization */ }

    internal KRCheckIn(
        Guid id,
        Guid keyResultId,
        decimal value,
        decimal confidenceLevel,
        string? notes,
        Guid updatedBy) : base(id)
    {
        KeyResultId = keyResultId;
        Value = value;
        ConfidenceLevel = Math.Clamp(confidenceLevel, 0m, 100m);
        Notes = notes;
        UpdatedBy = updatedBy;
        OccurredOnUtc = DateTimeOffset.UtcNow;
    }

    public Guid KeyResultId { get; private set; }
    public decimal Value { get; private set; }

    /// <summary>0–100. Subjective confidence that KR will be achieved on time.</summary>
    public decimal ConfidenceLevel { get; private set; }
    public string? Notes { get; private set; }
    public Guid UpdatedBy { get; private set; }
    public DateTimeOffset OccurredOnUtc { get; private set; }
}
