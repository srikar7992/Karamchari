using System;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Recruitment.Domain.Pipeline;

/// <summary>
/// Tenant-specific recruitment workflow pipeline definition.
/// </summary>
public sealed class RecruitmentWorkflowDefinition : AggregateRoot<Guid>, ITenantOwned
{
    private RecruitmentWorkflowDefinition()
    {
        TenantId = string.Empty;
        Name = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RecruitmentWorkflowDefinition"/> class.
    /// </summary>
    public RecruitmentWorkflowDefinition(Guid id, string tenantId, string name) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        TenantId = tenantId;
        Name = name;
        IsActive = true;
    }

    /// <inheritdoc />
    public string TenantId { get; private set; }

    /// <summary>
    /// Gets the name of the workflow layout.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the workflow is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Deactivates the workflow definition.
    /// </summary>
    public void Deactivate() => IsActive = false;
}

/// <summary>
/// Represents a stage (e.g. Assessment, HM Review) in a customizable recruitment pipeline.
/// </summary>
public sealed class RecruitmentWorkflowState : Entity<Guid>
{
    private RecruitmentWorkflowState()
    {
        StateName = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RecruitmentWorkflowState"/> class.
    /// </summary>
    public RecruitmentWorkflowState(Guid id, Guid workflowDefinitionId, string stateName, int order) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stateName);

        WorkflowDefinitionId = workflowDefinitionId;
        StateName = stateName;
        Order = order;
    }

    /// <summary>
    /// Gets the parent workflow layout ID.
    /// </summary>
    public Guid WorkflowDefinitionId { get; private set; }

    /// <summary>
    /// Gets the state or step display name.
    /// </summary>
    public string StateName { get; private set; }

    /// <summary>
    /// Gets the relative ordering position.
    /// </summary>
    public int Order { get; private set; }
}

/// <summary>
/// Models state change rules between custom candidate workflow pipeline steps.
/// </summary>
public sealed class RecruitmentWorkflowTransition : Entity<Guid>
{
    private RecruitmentWorkflowTransition()
    {
        TriggerName = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RecruitmentWorkflowTransition"/> class.
    /// </summary>
    public RecruitmentWorkflowTransition(Guid id, Guid workflowDefinitionId, Guid fromStateId, Guid toStateId, string triggerName) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(triggerName);

        WorkflowDefinitionId = workflowDefinitionId;
        FromStateId = fromStateId;
        ToStateId = toStateId;
        TriggerName = triggerName;
    }

    /// <summary>
    /// Gets the parent workflow layout ID.
    /// </summary>
    public Guid WorkflowDefinitionId { get; private set; }

    /// <summary>
    /// Gets the starting state ID boundary.
    /// </summary>
    public Guid FromStateId { get; private set; }

    /// <summary>
    /// Gets the destination state ID boundary.
    /// </summary>
    public Guid ToStateId { get; private set; }

    /// <summary>
    /// Gets the trigger command name (e.g. Reject, SchedulePanel).
    /// </summary>
    public string TriggerName { get; private set; }
}
