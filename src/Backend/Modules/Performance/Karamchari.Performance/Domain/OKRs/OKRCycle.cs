using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.OKRs;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class OKRCycle : AggregateRoot<Guid>, ITenantOwned
{
    private OKRCycle() { /* EF materialization */ }

    private OKRCycle(
        Guid id,
        string tenantId,
        string label,
        OKRCycleScope scope,
        DateOnly startDate,
        DateOnly endDate) : base(id)
    {
        TenantId = tenantId;
        Label = label;
        Scope = scope;
        StartDate = startDate;
        EndDate = endDate;
        Status = OKRCycleStatus.Draft;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Label { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public OKRCycleScope Scope { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly StartDate { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly EndDate { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public OKRCycleStatus Status { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static OKRCycle Create(
        string tenantId,
        string label,
        OKRCycleScope scope,
        DateOnly startDate,
        DateOnly endDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        if (endDate <= startDate)
            throw new ArgumentException("EndDate must be after StartDate.");

        return new OKRCycle(Guid.NewGuid(), tenantId, label.Trim(), scope, startDate, endDate);
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Activate()
    {
        if (Status != OKRCycleStatus.Draft)
            throw new InvalidOperationException($"Cannot activate cycle in state {Status}.");
        Status = OKRCycleStatus.Active;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void BeginScoring()
    {
        if (Status != OKRCycleStatus.Active)
            throw new InvalidOperationException($"Cannot begin scoring for cycle in state {Status}.");
        Status = OKRCycleStatus.Scoring;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Lock()
    {
        if (Status != OKRCycleStatus.Scoring)
            throw new InvalidOperationException($"Cannot lock cycle in state {Status}.");
        Status = OKRCycleStatus.Locked;
    }
}
