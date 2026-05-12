using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Workflow.Domain;

/// <summary>
/// Defines a template for a coordination workflow (approvals, routing, SLA).
/// Focuses exclusively on coordination; business truth remains in domain aggregates.
/// </summary>
public sealed class WorkflowDefinition : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<WorkflowStep> _steps = [];

    private WorkflowDefinition(Guid id, string tenantId, string entityType, string name)
        : base(id)
    {
        TenantId = tenantId;
        EntityType = entityType;
        Name = name;
        IsActive = true;
    }

    private WorkflowDefinition() { TenantId = string.Empty; EntityType = string.Empty; Name = string.Empty; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string EntityType { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Name { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>Higher priority definitions are evaluated first when multiple definitions match.</summary>
    public int Priority { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyList<WorkflowStep> Steps => _steps.AsReadOnly();

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static WorkflowDefinition Create(string tenantId, string entityType, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new WorkflowDefinition(Guid.NewGuid(), tenantId.Trim(), entityType.Trim(), name.Trim());
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void AddStep(WorkflowStep step)
    {
        ArgumentNullException.ThrowIfNull(step);

        if (_steps.Any(s => s.Order == step.Order))
            throw new InvalidOperationException(
                $"A step with order {step.Order} already exists in definition {Id}.");

        _steps.Add(step);
        _steps.Sort((a, b) => a.Order.CompareTo(b.Order));
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void SetPriority(int priority) => Priority = priority;
}
