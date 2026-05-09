using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.Career;

/// <summary>
/// An employee's active development plan toward their target CareerLevel.
/// TargetLevelId is advisory â€” references CareerLevel.Id. Not enforced as FK (cross-entity in same BC).
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EmployeeId { get; private set; }
    public Guid? TargetLevelId { get; private set; }
    public string? TargetLevelDisplay { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly TargetDate { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public GrowthPlanStatus Status { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyList<DevelopmentAction> Actions => _actions.AsReadOnly();

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Activate()
    {
        if (Status != GrowthPlanStatus.Draft)
            throw new InvalidOperationException($"Cannot activate from {Status}.");
        Status = GrowthPlanStatus.Active;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void StartAction(Guid actionId) => FindAction(actionId).Start();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void CompleteAction(Guid actionId, string? notes) => FindAction(actionId).Complete(notes);
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void DeferAction(Guid actionId) => FindAction(actionId).Defer();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void CancelAction(Guid actionId) => FindAction(actionId).Cancel();

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void MarkComplete()
    {
        if (Status != GrowthPlanStatus.Active)
            throw new InvalidOperationException($"Cannot mark complete from {Status}.");
        Status = GrowthPlanStatus.Completed;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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
