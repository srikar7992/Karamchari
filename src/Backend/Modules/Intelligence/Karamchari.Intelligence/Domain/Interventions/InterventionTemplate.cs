// -----------------------------------------------------------------------
// <copyright file="InterventionTemplate.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Intelligence.Domain.Interventions;

/// <summary>
/// Broad category of workforce intervention.
/// </summary>
public enum InterventionCategory
{
    WorkloadManagement,
    LeaveManagement,
    ManagerRelationship,
    TeamRestructuring,
    Compensation,
    Recognition,
    Development,
    Wellness,
    Other
}

/// <summary>
/// HR-managed catalog of intervention types that can be recommended for workforce risk signals.
/// Each template describes what action to take, its expected outcome, and workflow steps.
/// Per-signal effectiveness is tracked separately in <see cref="InterventionEffectiveness"/>.
/// Maps to Intel_InterventionTemplates.
/// </summary>
public sealed class InterventionTemplate : AggregateRoot<Guid>, ITenantOwned
{
    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Display name shown to managers and HR.</summary>
    public string Name { get; private set; } = string.Empty;

    public InterventionCategory Category { get; private set; }

    /// <summary>Explanation of the intervention and its rationale.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>JSON-serialised workflow steps for the intervention (checklist, approvals, etc.).</summary>
    public string WorkflowTemplateJson { get; private set; } = "{}";

    /// <summary>JSON-serialised preconditions that must be met before this template applies.</summary>
    public string PreconditionsJson { get; private set; } = "[]";

    /// <summary>Inactive templates are hidden from new recommendation generation.</summary>
    public bool IsActive { get; private set; } = true;

    public DateTime CreatedAt { get; private set; }

    public string CreatedBy { get; private set; } = string.Empty;

    public DateTime? UpdatedAt { get; private set; }

    private InterventionTemplate() { }

    public static InterventionTemplate Create(
        string tenantId,
        string name,
        InterventionCategory category,
        string description,
        string createdBy,
        string workflowTemplateJson = "{}",
        string preconditionsJson = "[]")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        return new InterventionTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Category = category,
            Description = description,
            WorkflowTemplateJson = workflowTemplateJson,
            PreconditionsJson = preconditionsJson,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateWorkflow(string workflowTemplateJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowTemplateJson);
        WorkflowTemplateJson = workflowTemplateJson;
        UpdatedAt = DateTime.UtcNow;
    }
}
