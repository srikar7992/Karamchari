using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.OKRs;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class KeyResult : Entity<Guid>
{
    private readonly List<KRCheckIn> _checkIns = [];
    private KeyResult() { /* EF materialization */ }

    internal KeyResult(
        Guid id,
        Guid objectiveId,
        string title,
        KRType type,
        decimal target,
        string unit,
        decimal weight,
        Guid? dependsOnKrId) : base(id)
    {
        ObjectiveId = objectiveId;
        Title = title;
        Type = type;
        Target = target;
        Unit = unit;
        Weight = weight;
        DependsOnKrId = dependsOnKrId;
        CurrentValue = 0m;
        ProgressPercent = 0m;
        ConfidenceLevel = 100m;
        Score = 0m;
        Status = KRStatus.NotStarted;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ObjectiveId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Title { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public KRType Type { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal Target { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Unit { get; private set; } = string.Empty;

    /// <summary>0â€“100. Sum of weights within an Objective must equal 100.</summary>
    public decimal Weight { get; private set; }
    public Guid? DependsOnKrId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal CurrentValue { get; private set; }

    /// <summary>0â€“140. Allows 140% for stretch goals.</summary>
    public decimal ProgressPercent { get; private set; }

    /// <summary>0â€“100 subjective on-track confidence.</summary>
    public decimal ConfidenceLevel { get; private set; }

    /// <summary>0â€“1 normalized score. Set at cycle scoring.</summary>
    public decimal Score { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public KRStatus Status { get; private set; }
    public DateTimeOffset? LastCheckInUtc { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyCollection<KRCheckIn> CheckIns => _checkIns.AsReadOnly();

    internal void RecordCheckIn(decimal value, decimal confidenceLevel, string? notes, Guid updatedBy)
    {
        var checkIn = new KRCheckIn(Guid.NewGuid(), Id, value, confidenceLevel, notes, updatedBy);
        _checkIns.Add(checkIn);

        CurrentValue = value;
        ConfidenceLevel = Math.Clamp(confidenceLevel, 0m, 100m);
        LastCheckInUtc = DateTimeOffset.UtcNow;

        ProgressPercent = Target == 0
            ? 0m
            : Math.Round(value / Target * 100m, 2);

        Status = ProgressPercent switch
        {
            >= 100m => KRStatus.Completed,
            _ when ConfidenceLevel >= 70m => KRStatus.OnTrack,
            _ when ConfidenceLevel >= 40m => KRStatus.AtRisk,
            _ => KRStatus.OffTrack
        };
    }

    internal void ApplyConfidenceDecay(decimal decayFactor)
    {
        ConfidenceLevel = Math.Max(0m, Math.Round(ConfidenceLevel * decayFactor, 2));
        if (ConfidenceLevel < 40m && Status == KRStatus.OnTrack)
            Status = KRStatus.AtRisk;
    }

    /// <summary>
    /// Computes normalized score (0â€“1) using standard OKR scoring formula.
    /// Metric: min(CurrentValue / Target, 1.4) / 1.4 normalized to 0â€“1.
    /// Binary: 0 or 1. Milestone: weighted checklist completion.
    /// </summary>
    internal void ComputeScore()
    {
        Score = Type switch
        {
            KRType.Metric => Target == 0
                ? 0m
                : Math.Min(Math.Round(CurrentValue / Target, 4), 1.4m) / 1.4m,
            KRType.Binary => CurrentValue >= Target ? 1m : 0m,
            KRType.Milestone => ProgressPercent / 100m,
            _ => 0m
        };
    }
}
