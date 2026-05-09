using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Governance.Domain.Incidents;

public enum IncidentSeverity
{
    Sev1_Critical,  // Platform Down, Data Loss
    Sev2_High,      // Major feature degraded, no workaround
    Sev3_Medium,    // Feature degraded, workaround exists
    Sev4_Low        // Minor glitch, UI issue
}

public enum IncidentStatus
{
    Investigating,
    Identified,
    Monitoring,
    Resolved,
    PostmortemRequired,
    Closed
}

/// <summary>
/// Aggregate root governing operational incidents.
/// Enforces Blameless Postmortem workflows for Sev1/Sev2 incidents.
/// </summary>
public sealed class OperationalIncident : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public IncidentSeverity Severity { get; private set; }
    public IncidentStatus Status { get; private set; }
    
    public DateTimeOffset DeclaredAtUtc { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }
    public TimeSpan? TimeToResolution => ResolvedAtUtc - DeclaredAtUtc;

    public string AffectedComponent { get; private set; } = string.Empty;
    public string? RootCauseSummary { get; private set; }
    public string? ActionItemsJson { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    private OperationalIncident() { }

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

    public void Resolve()
    {
        if (Status == IncidentStatus.Resolved || Status == IncidentStatus.Closed)
            throw new InvalidOperationException("Incident is already resolved.");

        ResolvedAtUtc = DateTimeOffset.UtcNow;
        Status = Severity is IncidentSeverity.Sev1_Critical or IncidentSeverity.Sev2_High 
            ? IncidentStatus.PostmortemRequired 
            : IncidentStatus.Resolved;
    }

    public void CompletePostmortem(string rootCause, string actionItemsJson)
    {
        if (Status != IncidentStatus.PostmortemRequired)
            throw new InvalidOperationException("Postmortem is not currently required.");

        RootCauseSummary = rootCause;
        ActionItemsJson = actionItemsJson;
        Status = IncidentStatus.Closed;
    }
}
