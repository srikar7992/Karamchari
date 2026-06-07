using System.Text.Json;
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
        ConditionsJson = "[]";
    }

    private WorkflowDefinition() { TenantId = string.Empty; EntityType = string.Empty; Name = string.Empty; ConditionsJson = "[]"; }

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
    /// JSON-serialized array of <see cref="WorkflowCondition"/>. Persisted; deserialized on demand.
    /// An empty array means the definition matches all routing requests of the correct EntityType.
    /// </summary>
    public string ConditionsJson { get; private set; }

    /// <summary>
    /// Deserialized routing conditions. Not mapped to a DB column (computed from <see cref="ConditionsJson"/>).
    /// </summary>
    public IReadOnlyList<WorkflowCondition> Conditions =>
        JsonSerializer.Deserialize<List<WorkflowCondition>>(ConditionsJson) ?? [];

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
    /// Adds a routing condition. All conditions must hold (AND semantics) for this definition to match.
    /// </summary>
    public void AddCondition(WorkflowCondition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        var list = JsonSerializer.Deserialize<List<WorkflowCondition>>(ConditionsJson) ?? [];
        list.Add(condition);
        ConditionsJson = JsonSerializer.Serialize(list);
    }

    /// <summary>
    /// Removes all conditions, making this definition match every request of the correct EntityType.
    /// </summary>
    public void ClearConditions() => ConditionsJson = "[]";

    /// <summary>
    /// Returns <c>true</c> when all conditions are satisfied by <paramref name="context"/>.
    /// A definition with no conditions always matches.
    /// </summary>
    public bool Matches(IReadOnlyDictionary<string, object>? context)
    {
        var conditions = Conditions;
        if (conditions.Count == 0) return true;
        if (context is null) return false;
        return conditions.All(c => c.Evaluate(context));
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
