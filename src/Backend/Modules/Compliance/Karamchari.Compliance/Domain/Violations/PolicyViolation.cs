using Karamchari.Compliance.Contracts;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Compliance.Domain.Violations;

/// <summary>
/// A detected policy or regulatory violation for a specific employee.
/// Created by the violation detection engine; never modified by source systems.
/// </summary>
public sealed class PolicyViolation : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public string PolicyCode { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public ViolationSeverity Severity { get; private set; }
    public ViolationStatus Status { get; private set; }
    public DateTime DetectedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? Notes { get; private set; }
    public string? EvidenceJson { get; private set; }

    private PolicyViolation() { }

    public static PolicyViolation Detect(
        string tenantId,
        string policyCode,
        Guid employeeId,
        ViolationSeverity severity,
        string evidenceJson)
    {
        var violation = new PolicyViolation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PolicyCode = policyCode,
            EmployeeId = employeeId,
            Severity = severity,
            Status = ViolationStatus.Open,
            DetectedAt = DateTime.UtcNow,
            EvidenceJson = evidenceJson
        };
        violation.RaiseDomainEvent(new PolicyViolationDetectedEvent(
            violation.Id, tenantId, policyCode, employeeId,
            severity.ToString(), violation.DetectedAt));
        return violation;
    }

    public void Resolve(string notes)
    {
        Status = ViolationStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
        Notes = notes;
        RaiseDomainEvent(new PolicyViolationResolvedEvent(
            Id, TenantId, PolicyCode, EmployeeId, notes, ResolvedAt.Value));
    }

    public void AcceptRisk(string notes)
    {
        Status = ViolationStatus.AcceptedRisk;
        Notes = notes;
    }

    public void StartInvestigation()
    {
        Status = ViolationStatus.Investigating;
    }
}
