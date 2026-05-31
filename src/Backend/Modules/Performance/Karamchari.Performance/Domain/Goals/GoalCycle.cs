using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Performance.Domain.Goals.Events;

namespace Karamchari.Performance.Domain.Goals;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public GoalCycleType Type { get; private set; }
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
    public GoalCycleStatus Status { get; private set; }

    /// <summary>Optimistic concurrency token.</summary>
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Activate()
    {
        if (Status != GoalCycleStatus.Draft)
            throw new InvalidOperationException($"Cannot activate a cycle in state {Status}.");

        Status = GoalCycleStatus.Active;
        RaiseDomainEvent(new GoalCycleActivated(Id, TenantId, Name, StartDate, EndDate, DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Lock()
    {
        if (Status != GoalCycleStatus.Active)
            throw new InvalidOperationException($"Cannot lock a cycle in state {Status}.");

        Status = GoalCycleStatus.Locked;
        RaiseDomainEvent(new GoalCycleLocked(Id, TenantId, DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Archive()
    {
        if (Status == GoalCycleStatus.Archived)
            return;

        Status = GoalCycleStatus.Archived;
    }
}
