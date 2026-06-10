// -----------------------------------------------------------------------
// <copyright file="WorkflowDefinition.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Workflow.Domain;

/// <summary>
/// Policy lifecycle state for a <see cref="WorkflowDefinition"/>.
/// Governance flow: Draft → InReview → Approved → Published → Deprecated.
/// </summary>
public enum PolicyStatus
{
    /// <summary>Work-in-progress. Not routable.</summary>
    Draft,
    /// <summary>Submitted for approval. Not routable.</summary>
    InReview,
    /// <summary>Approved but not yet effective. Not routable.</summary>
    Approved,
    /// <summary>Live and routable (subject to effective-date window).</summary>
    Published,
    /// <summary>Retired. Not routable.</summary>
    Deprecated,
}

/// <summary>
/// Defines a template for a coordination workflow (approvals, routing, SLA).
/// Focuses exclusively on coordination; business truth remains in domain aggregates.
/// </summary>
public sealed class WorkflowDefinition : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<WorkflowStep> _steps = [];
    private readonly List<WorkflowDefinitionApprovalEntry> _approvalHistory = [];

    private WorkflowDefinition(Guid id, string tenantId, string entityType, string name)
        : base(id)
    {
        TenantId = tenantId;
        EntityType = entityType;
        Name = name;
        IsActive = true;
        ConditionsJson = "[]";
        Status = PolicyStatus.Published; // backward-compatible default: new definitions are immediately routable
    }

    private WorkflowDefinition()
    {
        TenantId = string.Empty;
        EntityType = string.Empty;
        Name = string.Empty;
        ConditionsJson = "[]";
    }

    /// <summary>Tenant that owns this definition.</summary>
    public string TenantId { get; private set; }

    /// <summary>Entity type this definition applies to (e.g. "Expense", "LeaveRequest").</summary>
    public string EntityType { get; private set; }

    /// <summary>Human-readable name for the definition.</summary>
    public string Name { get; private set; }

    /// <summary>
    /// When false the definition is excluded from routing regardless of conditions.
    /// Managed by <see cref="Deactivate"/> and <see cref="Publish"/>.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>Higher priority definitions are evaluated first when multiple definitions match.</summary>
    public int Priority { get; private set; }

    /// <summary>Policy governance lifecycle state.</summary>
    public PolicyStatus Status { get; private set; }

    /// <summary>Date from which this definition is effective (inclusive). Null = no lower bound.</summary>
    public DateTime? EffectiveFrom { get; private set; }

    /// <summary>Date until which this definition is effective (inclusive). Null = no upper bound.</summary>
    public DateTime? EffectiveTo { get; private set; }

    /// <summary>
    /// JSON-serialized array of <see cref="WorkflowCondition"/>. Retained for backward compatibility
    /// with Phase 12 data. When <see cref="ExpressionJson"/> is set it takes precedence.
    /// </summary>
    public string ConditionsJson { get; private set; }

    /// <summary>
    /// JSON-serialized <see cref="ConditionGroup"/> supporting nested AND/OR/NOT expressions.
    /// When non-null this field drives routing; <see cref="ConditionsJson"/> is ignored.
    /// </summary>
    public string? ExpressionJson { get; private set; }

    /// <summary>
    /// Resolved routing expression. When <see cref="ExpressionJson"/> is set it is deserialized;
    /// otherwise the legacy flat <see cref="ConditionsJson"/> list is wrapped in an AND group.
    /// Not mapped to a DB column.
    /// </summary>
    public ConditionGroup Expression
    {
        get
        {
            if (ExpressionJson is not null)
                return JsonSerializer.Deserialize<ConditionGroup>(ExpressionJson) ?? ConditionGroup.Empty;

            var flatConditions = JsonSerializer.Deserialize<List<WorkflowCondition>>(ConditionsJson) ?? [];
            return flatConditions.Count == 0
                ? ConditionGroup.Empty
                : ConditionGroup.FromFlatList(flatConditions);
        }
    }

    /// <summary>Deserialized routing conditions (legacy flat view). Not mapped to a DB column.</summary>
    public IReadOnlyList<WorkflowCondition> Conditions =>
        JsonSerializer.Deserialize<List<WorkflowCondition>>(ConditionsJson) ?? [];

    /// <summary>Ordered steps of this workflow definition.</summary>
    public IReadOnlyList<WorkflowStep> Steps => _steps.AsReadOnly();

    /// <summary>
    /// Immutable audit trail of policy lifecycle transitions (SubmitForReview → Approve → Publish → Deprecate).
    /// Regulators can answer: who approved, when, and why.
    /// </summary>
    public IReadOnlyList<WorkflowDefinitionApprovalEntry> ApprovalHistory => _approvalHistory.AsReadOnly();

    // ── Factory ──────────────────────────────────────────────────────────────

    /// <summary>Creates a new definition. Defaults to <see cref="PolicyStatus.Published"/> for backward compatibility.</summary>
    public static WorkflowDefinition Create(string tenantId, string entityType, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new WorkflowDefinition(Guid.NewGuid(), tenantId.Trim(), entityType.Trim(), name.Trim());
    }

    /// <summary>Creates a new definition in <see cref="PolicyStatus.Draft"/> state (governance flow).</summary>
    public static WorkflowDefinition CreateDraft(string tenantId, string entityType, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var def = new WorkflowDefinition(Guid.NewGuid(), tenantId.Trim(), entityType.Trim(), name.Trim());
        def.Status = PolicyStatus.Draft;
        def.IsActive = false; // drafts are not routable
        return def;
    }

    // ── Steps ────────────────────────────────────────────────────────────────

    /// <summary>Appends a step to this definition.</summary>
    public void AddStep(WorkflowStep step)
    {
        ArgumentNullException.ThrowIfNull(step);

        if (_steps.Any(s => s.Order == step.Order))
            throw new InvalidOperationException(
                $"A step with order {step.Order} already exists in definition {Id}.");

        _steps.Add(step);
        _steps.Sort((a, b) => a.Order.CompareTo(b.Order));
    }

    // ── Legacy flat conditions (Phase 12 API) ────────────────────────────────

    /// <summary>
    /// Adds a routing condition using legacy AND-flat semantics.
    /// Use <see cref="SetExpression"/> for OR/NOT/nested expressions.
    /// </summary>
    public void AddCondition(WorkflowCondition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        var list = JsonSerializer.Deserialize<List<WorkflowCondition>>(ConditionsJson) ?? [];
        list.Add(condition);
        ConditionsJson = JsonSerializer.Serialize(list);
        ExpressionJson = null; // clear expression; flat list takes precedence when expression is null
    }

    /// <summary>Removes all conditions, making this definition match every request of the correct EntityType.</summary>
    public void ClearConditions()
    {
        ConditionsJson = "[]";
        ExpressionJson = null;
    }

    // ── Expression engine (Phase 13 API) ─────────────────────────────────────

    /// <summary>
    /// Sets a composed boolean expression (supports AND/OR/NOT nesting).
    /// Replaces any existing flat conditions.
    /// </summary>
    public void SetExpression(ConditionGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);
        ExpressionJson = JsonSerializer.Serialize(group);
        ConditionsJson = "[]"; // keep in sync for API consumers that read ConditionsJson directly
    }

    // ── Routing ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> when the expression matches <paramref name="context"/>.
    /// An empty expression (no conditions, no groups) always matches.
    /// </summary>
    public bool Matches(IReadOnlyDictionary<string, object>? context)
    {
        var expr = Expression;

        // Empty expression = unconditional match (backward compatible).
        if (expr.Conditions.Count == 0 && expr.Groups.Count == 0)
            return true;

        if (context is null)
            return false;

        return expr.Evaluate(context);
    }

    // ── Policy lifecycle ──────────────────────────────────────────────────────

    /// <summary>Submits this definition for review. Only valid from <see cref="PolicyStatus.Draft"/>.</summary>
    public void SubmitForReview(string actorId, string? notes = null)
    {
        if (Status != PolicyStatus.Draft)
            throw new InvalidOperationException(
                $"Cannot submit definition {Id} for review: current status is {Status}.");
        RecordTransition(actorId, PolicyStatus.Draft, PolicyStatus.InReview, notes);
        Status = PolicyStatus.InReview;
    }

    /// <summary>Approves this definition. Only valid from <see cref="PolicyStatus.InReview"/>.</summary>
    public void Approve(string actorId, string? notes = null)
    {
        if (Status != PolicyStatus.InReview)
            throw new InvalidOperationException(
                $"Cannot approve definition {Id}: current status is {Status}.");
        RecordTransition(actorId, PolicyStatus.InReview, PolicyStatus.Approved, notes);
        Status = PolicyStatus.Approved;
    }

    /// <summary>
    /// Publishes this definition, making it routable.
    /// Activates the definition and clears any previous <see cref="PolicyStatus.Deprecated"/> state.
    /// Valid from <see cref="PolicyStatus.Approved"/> or <see cref="PolicyStatus.Draft"/> (fast-path).
    /// </summary>
    public void Publish(string actorId, string? notes = null)
    {
        if (Status != PolicyStatus.Approved && Status != PolicyStatus.Draft)
            throw new InvalidOperationException(
                $"Cannot publish definition {Id}: must be Approved or Draft (got {Status}).");
        RecordTransition(actorId, Status, PolicyStatus.Published, notes);
        Status = PolicyStatus.Published;
        IsActive = true;
    }

    /// <summary>Retires this definition. Deactivates routing.</summary>
    public void Deprecate(string actorId, string? notes = null)
    {
        RecordTransition(actorId, Status, PolicyStatus.Deprecated, notes);
        Status = PolicyStatus.Deprecated;
        IsActive = false;
    }

    /// <summary>Retracts to Draft for corrections. Deactivates routing.</summary>
    public void RetractToDraft(string actorId, string? notes = null)
    {
        RecordTransition(actorId, Status, PolicyStatus.Draft, notes);
        Status = PolicyStatus.Draft;
        IsActive = false;
    }

    private void RecordTransition(string actorId, PolicyStatus from, PolicyStatus to, string? notes)
    {
        _approvalHistory.Add(new WorkflowDefinitionApprovalEntry(
            Guid.NewGuid(), actorId, from, to, DateTimeOffset.UtcNow, notes));
    }

    // ── Effective dates ────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the effective date window.
    /// <paramref name="from"/> and <paramref name="to"/> are both inclusive; pass null to remove the bound.
    /// </summary>
    public void SetEffectiveDates(DateTime? from, DateTime? to)
    {
        if (from.HasValue && to.HasValue && from.Value > to.Value)
            throw new ArgumentException("EffectiveFrom must not be later than EffectiveTo.");
        EffectiveFrom = from;
        EffectiveTo = to;
    }

    // ── Misc ──────────────────────────────────────────────────────────────────

    /// <summary>Deactivates routing without changing policy status.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Sets the routing priority.</summary>
    public void SetPriority(int priority) => Priority = priority;
}
