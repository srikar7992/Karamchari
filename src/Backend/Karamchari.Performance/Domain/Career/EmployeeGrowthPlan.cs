using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.Career;

/// <summary>
/// An employee's active development plan toward their target CareerLevel.
/// TargetLevelId is advisory — references CareerLevel.Id. Not enforced as FK (cross-entity in same BC).
/// </summary>
public sealed class EmployeeGrowthPlan : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<DevelopmentAction> _actions = [];

    private EmployeeGrowthPlan() { /* EF materialization */ }

    private EmployeeGrowthPlan(
        Guid id,
        string tenantId,
        Guid employeeId,
        Guid? targetLevelId,
        string? targetLevelDisplay,
        DateOnly targetDate) : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        TargetLevelId = targetLevelId;
        TargetLevelDisplay = targetLevelDisplay;
        TargetDate = targetDate;
        Status = GrowthPlanStatus.Draft;
    }

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public Guid? TargetLevelId { get; private set; }
    public string? TargetLevelDisplay { get; private set; }
    public DateOnly TargetDate { get; private set; }
    public GrowthPlanStatus Status { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyList<DevelopmentAction> Actions => _actions.AsReadOnly();

    public static EmployeeGrowthPlan Create(
        string tenantId,
        Guid employeeId,
        Guid? targetLevelId,
        string? targetLevelDisplay,
        DateOnly targetDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        if (targetDate <= DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("TargetDate must be in the future.");

        return new EmployeeGrowthPlan(Guid.NewGuid(), tenantId, employeeId,
            targetLevelId, targetLevelDisplay?.Trim(), targetDate);
    }

    public DevelopmentAction AddAction(
        string title,
        DevelopmentActionType type,
        string? description = null,
        DateOnly? targetCompletionDate = null)
    {
        EnsureNotArchived();
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var action = new DevelopmentAction(Guid.NewGuid(), Id, title.Trim(), type,
            description?.Trim(), targetCompletionDate);
        _actions.Add(action);
        return action;
    }

    public void Activate()
    {
        if (Status != GrowthPlanStatus.Draft)
            throw new InvalidOperationException($"Cannot activate from {Status}.");
        Status = GrowthPlanStatus.Active;
    }

    public void StartAction(Guid actionId) => FindAction(actionId).Start();
    public void CompleteAction(Guid actionId, string? notes) => FindAction(actionId).Complete(notes);
    public void DeferAction(Guid actionId) => FindAction(actionId).Defer();
    public void CancelAction(Guid actionId) => FindAction(actionId).Cancel();

    public void MarkComplete()
    {
        if (Status != GrowthPlanStatus.Active)
            throw new InvalidOperationException($"Cannot mark complete from {Status}.");
        Status = GrowthPlanStatus.Completed;
    }

    public void Archive()
    {
        if (Status == GrowthPlanStatus.Archived)
            throw new InvalidOperationException("Already archived.");
        Status = GrowthPlanStatus.Archived;
    }

    private DevelopmentAction FindAction(Guid actionId)
        => _actions.FirstOrDefault(a => a.Id == actionId)
            ?? throw new InvalidOperationException($"Action {actionId} not found in this plan.");

    private void EnsureNotArchived()
    {
        if (Status == GrowthPlanStatus.Archived)
            throw new InvalidOperationException("Cannot modify an archived plan.");
    }
}
