// -----------------------------------------------------------------------
// <copyright file="CalibrationEntry.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.Calibration;

/// <summary>
/// One employee's record within a CalibrationSession.
/// Tracks pre-calibration â†’ calibrated score + bucket.
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid SessionId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EmployeeId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal PreCalibrationScore { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string PreCalibrationBucket { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal CalibratedScore { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string CalibratedBucket { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsFinalized { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyList<CalibrationAdjustmentRecord> Adjustments => _adjustments.AsReadOnly();

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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
