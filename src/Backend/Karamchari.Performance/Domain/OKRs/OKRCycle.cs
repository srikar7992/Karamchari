using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.OKRs;

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

    public string TenantId { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public OKRCycleScope Scope { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public OKRCycleStatus Status { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

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

    public void Activate()
    {
        if (Status != OKRCycleStatus.Draft)
            throw new InvalidOperationException($"Cannot activate cycle in state {Status}.");
        Status = OKRCycleStatus.Active;
    }

    public void BeginScoring()
    {
        if (Status != OKRCycleStatus.Active)
            throw new InvalidOperationException($"Cannot begin scoring for cycle in state {Status}.");
        Status = OKRCycleStatus.Scoring;
    }

    public void Lock()
    {
        if (Status != OKRCycleStatus.Scoring)
            throw new InvalidOperationException($"Cannot lock cycle in state {Status}.");
        Status = OKRCycleStatus.Locked;
    }
}
