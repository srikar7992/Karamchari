using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.OKRs;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid KeyResultId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal Value { get; private set; }

    /// <summary>0â€“100. Subjective confidence that KR will be achieved on time.</summary>
    public decimal ConfidenceLevel { get; private set; }
    public string? Notes { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid UpdatedBy { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; private set; }
}
