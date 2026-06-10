// -----------------------------------------------------------------------
// <copyright file="FutureOrgScenario.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.HR.Domain.Organization;

using System;
using System.Collections.Generic;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Represents a future organizational restructuring scenario planning aggregate.
/// </summary>
public sealed class FutureOrgScenario : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<ScenarioChange> _changes = [];

    private FutureOrgScenario() { TenantId = string.Empty; ScenarioName = string.Empty; Description = string.Empty; Status = "Draft"; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FutureOrgScenario"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the future org scenario.</param>
    /// <param name="tenantId">The unique identifier of the tenant context.</param>
    /// <param name="scenarioName">The descriptive name of the scenario.</param>
    /// <param name="description">An optional detailed description of the scenario.</param>
    /// <param name="targetExecutionDate">The target date for executing the changes.</param>
    public FutureOrgScenario(
        Guid id,
        string tenantId,
        string scenarioName,
        string description,
        DateTimeOffset targetExecutionDate) : base(id)
    {
        TenantId = tenantId;
        ScenarioName = scenarioName;
        Description = description ?? string.Empty;
        Status = "Draft";
        TargetExecutionDate = targetExecutionDate;
    }

    /// <summary>Gets the tenant ID for multi-tenant isolation.</summary>
    public string TenantId { get; private set; }
    /// <summary>Gets the name of the scenario.</summary>
    public string ScenarioName { get; private set; }
    /// <summary>Gets the description of the scenario.</summary>
    public string Description { get; private set; }
    /// <summary>Gets the status of the scenario (Draft, Approved, Executed, Cancelled).</summary>
    public string Status { get; private set; }
    /// <summary>Gets the target execution date for the scenario changes.</summary>
    public DateTimeOffset TargetExecutionDate { get; private set; }
    /// <summary>Gets the collection of proposed changes for this scenario.</summary>
    public IReadOnlyCollection<ScenarioChange> Changes => _changes.AsReadOnly();

    /// <summary>
    /// Adds a proposed change to the scenario.
    /// </summary>
    /// <param name="change">The scenario change to add.</param>
    public void AddChange(ScenarioChange change)
    {
        ArgumentNullException.ThrowIfNull(change);
        _changes.Add(change);
    }

    /// <summary>Approves the scenario.</summary>
    public void Approve() => Status = "Approved";
    /// <summary>Executes the scenario.</summary>
    public void Execute() => Status = "Executed";
    /// <summary>Cancels the scenario.</summary>
    public void Cancel() => Status = "Cancelled";
}
