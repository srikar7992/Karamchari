using Karamchari.Compliance.Contracts;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Compliance.Domain.Reporting;

/// <summary>
/// Evidence bundle requested by an auditor.
/// Contains employee record + attendance + payroll + approvals + audit trail + violations.
/// </summary>
public sealed class AuditPackage : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public string RequestedBy { get; private set; } = string.Empty;
    public Guid? EmployeeId { get; private set; }
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }
    public AuditPackageStatus Status { get; private set; }
    public string? PackageUri { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? FailureReason { get; private set; }

    private AuditPackage() { }

    public static AuditPackage Request(
        string tenantId,
        string requestedBy,
        Guid? employeeId,
        DateTime periodStart,
        DateTime periodEnd)
    {
        return new AuditPackage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RequestedBy = requestedBy,
            EmployeeId = employeeId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Status = AuditPackageStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };
    }

    public void MarkGenerating()
    {
        Status = AuditPackageStatus.Generating;
    }

    public void MarkReady(string packageUri)
    {
        Status = AuditPackageStatus.Ready;
        PackageUri = packageUri;
        CompletedAt = DateTime.UtcNow;
        RaiseDomainEvent(new AuditPackageGeneratedEvent(Id, TenantId, RequestedBy, packageUri, CompletedAt.Value));
    }

    public void MarkFailed(string reason)
    {
        Status = AuditPackageStatus.Failed;
        FailureReason = reason;
        CompletedAt = DateTime.UtcNow;
    }
}
