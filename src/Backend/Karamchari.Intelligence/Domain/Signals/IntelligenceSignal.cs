using System.Text.Json;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Intelligence.Domain.Primitives;

namespace Karamchari.Intelligence.Domain.Signals;

/// <summary>
/// Aggregate root representing an abstract, versioned intelligence signal.
/// Ensures all cross-domain analytics share a common schema, confidence rating, and lineage.
/// </summary>
public sealed class IntelligenceSignal : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public string SignalType { get; private set; } = string.Empty; // e.g., "WorkforceReadiness", "AttritionRisk"
    public string Version { get; private set; } = string.Empty;
    public string SubjectId { get; private set; } = string.Empty; // e.g., EmployeeId or DepartmentId
    
    public decimal Value { get; private set; }
    public SignalConfidence Confidence { get; private set; } = null!;
    
    /// <summary>
    /// A JSON string describing how this signal was calculated, who owns it,
    /// and what parent signals contributed to it. Ensures executive auditability.
    /// </summary>
    public string LineageData { get; private set; } = string.Empty;

    public DateTimeOffset GeneratedAtUtc { get; private set; }
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private IntelligenceSignal() { }

    public static IntelligenceSignal Emit(
        string tenantId,
        string signalType,
        string version,
        string subjectId,
        decimal value,
        SignalConfidence confidence,
        ScoreExplanation explanation,
        TimeSpan? timeToLive = null)
    {
        if (string.IsNullOrWhiteSpace(signalType)) throw new ArgumentException("Signal type is required.");
        if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("Version is required.");
        ArgumentNullException.ThrowIfNull(explanation);

        var now = DateTimeOffset.UtcNow;
        return new IntelligenceSignal
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SignalType = signalType,
            Version = version,
            SubjectId = subjectId,
            Value = value,
            Confidence = confidence,
            LineageData = JsonSerializer.Serialize(explanation),
            GeneratedAtUtc = now,
            ExpiresAtUtc = timeToLive.HasValue ? now.Add(timeToLive.Value) : null
        };
    }
}
