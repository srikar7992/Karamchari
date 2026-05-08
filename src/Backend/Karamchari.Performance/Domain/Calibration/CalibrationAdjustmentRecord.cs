using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Calibration;

/// <summary>
/// Immutable audit trail for each adjustment made to an employee's calibration score.
/// Append-only — never updated after creation.
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

    public Guid CalibrationEntryId { get; private set; }
    public decimal PreviousScore { get; private set; }
    public decimal NewScore { get; private set; }
    public string PreviousBucket { get; private set; } = string.Empty;
    public string NewBucket { get; private set; } = string.Empty;
    public Guid AdjustedBy { get; private set; }
    public string Justification { get; private set; } = string.Empty;
    public DateTimeOffset AdjustedOnUtc { get; private set; }
}
