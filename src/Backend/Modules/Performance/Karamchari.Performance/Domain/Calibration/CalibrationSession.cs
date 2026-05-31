using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Performance.Domain.Calibration.Events;

namespace Karamchari.Performance.Domain.Calibration;

/// <summary>
/// Manages one calibration panel session for a subset of employees within a review cycle.
/// Business truth is owned here; coordination (approval) is managed by Workflow.
/// </summary>
public sealed class CalibrationSession : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<CalibrationEntry> _entries = [];
    private readonly List<CalibrationPanelMember> _panelMembers = [];

    private CalibrationSession() { /* EF materialization */ }

    private CalibrationSession(
        Guid id,
        string tenantId,
        Guid reviewCycleId,
        string name,
        BellCurvePolicy bellCurvePolicy) : base(id)
    {
        TenantId = tenantId;
        ReviewCycleId = reviewCycleId;
        Name = name;
        BellCurvePolicy = bellCurvePolicy;
        Status = CalibrationSessionStatus.Draft;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ReviewCycleId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public BellCurvePolicy BellCurvePolicy { get; private set; } = null!;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public CalibrationSessionStatus Status { get; private set; }
    public DateTimeOffset? FinalizedOnUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyList<CalibrationEntry> Entries => _entries.AsReadOnly();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyList<CalibrationPanelMember> PanelMembers => _panelMembers.AsReadOnly();

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static CalibrationSession Create(
        string tenantId,
        Guid reviewCycleId,
        string name,
        BellCurvePolicy bellCurvePolicy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(bellCurvePolicy);
        return new CalibrationSession(Guid.NewGuid(), tenantId, reviewCycleId, name.Trim(), bellCurvePolicy);
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void AddPanelMember(Guid employeeId, CalibrationRole role)
    {
        EnsureNotFinalized();
        if (_panelMembers.Any(m => m.EmployeeId == employeeId))
            throw new InvalidOperationException($"Employee {employeeId} is already a panel member.");
        _panelMembers.Add(new CalibrationPanelMember(Guid.NewGuid(), Id, employeeId, role));
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void EnrollEmployee(Guid employeeId, decimal preCalibrationScore, string preCalibrationBucket)
    {
        EnsureNotFinalized();
        ArgumentException.ThrowIfNullOrWhiteSpace(preCalibrationBucket);
        if (_entries.Any(e => e.EmployeeId == employeeId))
            throw new InvalidOperationException($"Employee {employeeId} already enrolled.");
        _entries.Add(new CalibrationEntry(Guid.NewGuid(), TenantId, Id, employeeId,
            preCalibrationScore, preCalibrationBucket));
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Begin()
    {
        if (Status != CalibrationSessionStatus.Draft)
            throw new InvalidOperationException($"Cannot begin from {Status}.");
        if (_entries.Count == 0)
            throw new InvalidOperationException("Cannot begin calibration session with no enrolled employees.");
        Status = CalibrationSessionStatus.InProgress;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void AdjustEntry(Guid employeeId, decimal newScore, string newBucket, Guid adjustedBy, string justification)
    {
        if (Status != CalibrationSessionStatus.InProgress)
            throw new InvalidOperationException($"Adjustments only allowed in InProgress state.");
        var entry = _entries.FirstOrDefault(e => e.EmployeeId == employeeId)
            ?? throw new InvalidOperationException($"Employee {employeeId} not in this session.");
        entry.AdjustScore(newScore, newBucket, adjustedBy, justification);
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Submit()
    {
        if (Status != CalibrationSessionStatus.InProgress)
            throw new InvalidOperationException($"Cannot submit from {Status}.");

        // Business state remains InProgress until coordination completes.
    }

    /// <summary>
    /// Finalizes the calibration session, making adjusted scores authoritative.
    /// Called by Workflow coordination upon successful completion.
    /// </summary>
    public void FinalizeApproved()
    {
        if (Status != CalibrationSessionStatus.InProgress)
            throw new InvalidOperationException($"Cannot finalize from {Status}.");

        foreach (var entry in _entries)
            entry.FinalizeEntry();

        Status = CalibrationSessionStatus.Finalized;
        FinalizedOnUtc = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new CalibrationSessionFinalized(
            Id, TenantId, ReviewCycleId, _entries.Count, FinalizedOnUtc.Value));
    }

    /// <summary>
    /// Rejects the calibration session adjustments during coordination.
    /// </summary>
    public void FinalizeRejected()
    {
        if (Status != CalibrationSessionStatus.InProgress)
            throw new InvalidOperationException($"Cannot reject from {Status}.");

        Status = CalibrationSessionStatus.Rejected;
    }

    private void EnsureNotFinalized()
    {
        if (Status == CalibrationSessionStatus.Finalized || Status == CalibrationSessionStatus.Archived)
            throw new InvalidOperationException($"Cannot modify a {Status} calibration session.");
    }
}
