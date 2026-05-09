using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Intelligence.Domain.Primitives;

namespace Karamchari.Intelligence.Domain.Signals;

/// <summary>
/// Aggregate root representing an executive-level strategic insight.
/// Provides the "narrative" wrapper around multiple technical signals.
/// </summary>
public sealed class ExecutiveInsight : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Narrative { get; private set; } = string.Empty; // The explainable text
    
    public string Category { get; private set; } = string.Empty; // e.g., "Retention", "Growth"
    public string Impact { get; private set; } = string.Empty; // e.g., "High", "Critical"
    
    public IReadOnlyCollection<Guid> ContributingSignalIds { get; private set; } = [];
    
    public SignalConfidence AggregateConfidence { get; private set; } = null!;
    public DateTimeOffset GeneratedAtUtc { get; private set; }
    public bool IsAcknowledged { get; private set; }
    public string? AcknowledgedBy { get; private set; }

    private ExecutiveInsight() { }

    public static ExecutiveInsight Generate(
        string tenantId,
        string title,
        string narrative,
        string category,
        string impact,
        IEnumerable<Guid> signalIds,
        SignalConfidence confidence)
    {
        return new ExecutiveInsight
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = title,
            Narrative = narrative,
            Category = category,
            Impact = impact,
            ContributingSignalIds = signalIds.ToList().AsReadOnly(),
            AggregateConfidence = confidence,
            GeneratedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void Acknowledge(string userId)
    {
        IsAcknowledged = true;
        AcknowledgedBy = userId;
    }
}
