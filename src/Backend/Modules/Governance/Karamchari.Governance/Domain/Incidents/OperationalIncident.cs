// -----------------------------------------------------------------------
// <copyright file="OperationalIncident.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Governance.Domain.Incidents;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum IncidentSeverity
{
    /// <inheritdoc/>
    Sev1Critical,  // Platform Down, Data Loss
    /// <inheritdoc/>
    Sev2High,      // Major feature degraded, no workaround
    /// <inheritdoc/>
    Sev3Medium,    // Feature degraded, workaround exists
    /// <inheritdoc/>
    Sev4Low        // Minor glitch, UI issue
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum IncidentStatus
{
    /// <inheritdoc/>
    Investigating,
    /// <inheritdoc/>
    Identified,
    /// <inheritdoc/>
    Monitoring,
    /// <inheritdoc/>
    Resolved,
    /// <inheritdoc/>
    PostmortemRequired,
    /// <inheritdoc/>
    Closed
}

/// <summary>
/// Aggregate root governing operational incidents.
/// Enforces Blameless Postmortem workflows for Sev1/Sev2 incidents.
/// </summary>
public sealed class OperationalIncident : AggregateRoot<Guid>, ITenantOwned
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Title { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IncidentSeverity Severity { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IncidentStatus Status { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset DeclaredAtUtc { get; private set; }
    /// <inheritdoc/>
    public DateTimeOffset? ResolvedAtUtc { get; private set; }
    /// <inheritdoc/>
    public TimeSpan? TimeToResolution => ResolvedAtUtc - DeclaredAtUtc;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string AffectedComponent { get; private set; } = string.Empty;
    /// <inheritdoc/>
    public string? RootCauseSummary { get; private set; }
    /// <inheritdoc/>
    public string? ActionItemsJson { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    private OperationalIncident() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static OperationalIncident Declare(string tenantId, string title, IncidentSeverity severity, string component)
    {
        return new OperationalIncident
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = title,
            Severity = severity,
            Status = IncidentStatus.Investigating,
            AffectedComponent = component,
            DeclaredAtUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Resolve()
    {
        if (Status == IncidentStatus.Resolved || Status == IncidentStatus.Closed)
            throw new InvalidOperationException("Incident is already resolved.");

        ResolvedAtUtc = DateTimeOffset.UtcNow;
        Status = Severity is IncidentSeverity.Sev1Critical or IncidentSeverity.Sev2High
            ? IncidentStatus.PostmortemRequired
            : IncidentStatus.Resolved;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void CompletePostmortem(string rootCause, string actionItemsJson)
    {
        if (Status != IncidentStatus.PostmortemRequired)
            throw new InvalidOperationException("Postmortem is not currently required.");

        RootCauseSummary = rootCause;
        ActionItemsJson = actionItemsJson;
        Status = IncidentStatus.Closed;
    }
}
