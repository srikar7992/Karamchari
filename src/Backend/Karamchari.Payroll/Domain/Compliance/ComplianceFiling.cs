using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Payroll.Domain.Compliance;

public enum FilingStatus
{
    Pending,
    Filed,
    Failed
}

/// <summary>
/// Tracks the actual filing state of a compliance return on government portals.
/// </summary>
public sealed class ComplianceFiling : Entity<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public Guid PayrollRunId { get; private set; }
    public ComplianceType Type { get; private set; }
    public FilingStatus Status { get; private set; }
    public DateTime? FiledAtUtc { get; private set; }
    public string FiledBy { get; private set; } = string.Empty;
    public string ReferenceNumber { get; private set; } = string.Empty;
    public string Remarks { get; private set; } = string.Empty;
    public DateTime DueDate { get; private set; }

    private ComplianceFiling() { }

    public static ComplianceFiling CreatePending(
        string tenantId,
        Guid payrollRunId,
        ComplianceType type,
        DateTime dueDate)
    {
        return new ComplianceFiling
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PayrollRunId = payrollRunId,
            Type = type,
            Status = FilingStatus.Pending,
            DueDate = dueDate
        };
    }

    public void MarkAsFiled(string referenceNumber, string filedBy, string remarks = "")
    {
        Status = FilingStatus.Filed;
        FiledAtUtc = DateTime.UtcNow;
        ReferenceNumber = referenceNumber;
        FiledBy = filedBy;
        Remarks = remarks;
    }

    public void MarkAsFailed(string remarks)
    {
        Status = FilingStatus.Failed;
        Remarks = remarks;
    }
}
