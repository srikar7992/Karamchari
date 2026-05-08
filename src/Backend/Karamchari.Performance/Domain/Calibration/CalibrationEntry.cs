using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.Calibration;

/// <summary>
/// One employee's record within a CalibrationSession.
/// Tracks pre-calibration → calibrated score + bucket.
/// All adjustments are audit-logged in CalibrationAdjustmentRecords.
/// </summary>
public sealed class CalibrationEntry : Entity<Guid>, ITenantOwned
{
    private readonly List<CalibrationAdjustmentRecord> _adjustments = [];

    private CalibrationEntry() { /* EF materialization */ }

    internal CalibrationEntry(
        Guid id,
        string tenantId,
        Guid sessionId,
        Guid employeeId,
        decimal preCalibrationScore,
        string preCalibrationBucket) : base(id)
    {
        TenantId = tenantId;
        SessionId = sessionId;
        EmployeeId = employeeId;
        PreCalibrationScore = preCalibrationScore;
        PreCalibrationBucket = preCalibrationBucket;
        CalibratedScore = preCalibrationScore;
        CalibratedBucket = preCalibrationBucket;
        IsFinalized = false;
    }

    public string TenantId { get; private set; } = string.Empty;
    public Guid SessionId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public decimal PreCalibrationScore { get; private set; }
    public string PreCalibrationBucket { get; private set; } = string.Empty;
    public decimal CalibratedScore { get; private set; }
    public string CalibratedBucket { get; private set; } = string.Empty;
    public bool IsFinalized { get; private set; }
    public IReadOnlyList<CalibrationAdjustmentRecord> Adjustments => _adjustments.AsReadOnly();

    public void AdjustScore(decimal newScore, string newBucket, Guid adjustedBy, string justification)
    {
        if (IsFinalized)
            throw new InvalidOperationException("Cannot adjust a finalized calibration entry.");
        ArgumentException.ThrowIfNullOrWhiteSpace(newBucket);
        ArgumentException.ThrowIfNullOrWhiteSpace(justification);

        _adjustments.Add(new CalibrationAdjustmentRecord(
            Guid.NewGuid(), Id, CalibratedScore, newScore,
            CalibratedBucket, newBucket, adjustedBy, justification));

        CalibratedScore = newScore;
        CalibratedBucket = newBucket;
    }

    internal void FinalizeEntry() => IsFinalized = true;
}
