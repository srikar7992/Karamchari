using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Calibration;

/// <summary>
/// Immutable audit trail for each adjustment made to an employee's calibration score.
/// Append-only â€” never updated after creation.
/// </summary>
public sealed class CalibrationAdjustmentRecord : Entity<Guid>
{
    private CalibrationAdjustmentRecord() { /* EF materialization */ }

    internal CalibrationAdjustmentRecord(
        Guid id,
        Guid calibrationEntryId,
        decimal previousScore,
        decimal newScore,
        string previousBucket,
        string newBucket,
        Guid adjustedBy,
        string justification) : base(id)
    {
        CalibrationEntryId = calibrationEntryId;
        PreviousScore = previousScore;
        NewScore = newScore;
        PreviousBucket = previousBucket;
        NewBucket = newBucket;
        AdjustedBy = adjustedBy;
        Justification = justification;
        AdjustedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid CalibrationEntryId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal PreviousScore { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal NewScore { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string PreviousBucket { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string NewBucket { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid AdjustedBy { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Justification { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset AdjustedOnUtc { get; private set; }
}
