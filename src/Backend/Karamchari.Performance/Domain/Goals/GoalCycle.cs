using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Performance.Domain.Goals.Events;

namespace Karamchari.Performance.Domain.Goals;

public sealed class GoalCycle : AggregateRoot<Guid>, ITenantOwned
{
    private GoalCycle() { /* EF materialization */ }

    private GoalCycle(
        Guid id,
        string tenantId,
        string name,
        GoalCycleType type,
        DateOnly startDate,
        DateOnly endDate) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
        Status = GoalCycleStatus.Draft;
    }

    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public GoalCycleType Type { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public GoalCycleStatus Status { get; private set; }

    /// <summary>Optimistic concurrency token.</summary>
    public byte[] RowVersion { get; private set; } = [];

    public static GoalCycle Create(
        string tenantId,
        string name,
        GoalCycleType type,
        DateOnly startDate,
        DateOnly endDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (endDate <= startDate)
            throw new ArgumentException("EndDate must be after StartDate.");

        return new GoalCycle(Guid.NewGuid(), tenantId, name.Trim(), type, startDate, endDate);
    }

    public void Activate()
    {
        if (Status != GoalCycleStatus.Draft)
            throw new InvalidOperationException($"Cannot activate a cycle in state {Status}.");

        Status = GoalCycleStatus.Active;
        RaiseDomainEvent(new GoalCycleActivated(Id, TenantId, Name, StartDate, EndDate, DateTimeOffset.UtcNow));
    }

    public void Lock()
    {
        if (Status != GoalCycleStatus.Active)
            throw new InvalidOperationException($"Cannot lock a cycle in state {Status}.");

        Status = GoalCycleStatus.Locked;
        RaiseDomainEvent(new GoalCycleLocked(Id, TenantId, DateTimeOffset.UtcNow));
    }

    public void Archive()
    {
        if (Status == GoalCycleStatus.Archived)
            return;

        Status = GoalCycleStatus.Archived;
    }
}
