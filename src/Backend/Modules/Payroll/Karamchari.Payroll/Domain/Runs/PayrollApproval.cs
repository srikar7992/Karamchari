using Karamchari.Core.Multitenancy;

namespace Karamchari.Payroll.Domain.Runs;

/// <summary>
/// Explicit approval record for a payroll run. Financial systems require traceable approval.
/// One per PayrollRun. Immutable once created.
/// </summary>
public sealed class PayrollApproval : ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public Guid Id { get; private set; }
    public Guid PayrollRunId { get; private set; }
    public string ApprovedBy { get; private set; } = string.Empty;
    public DateTimeOffset ApprovedAt { get; private set; }
    public string? Comments { get; private set; }
    public decimal TotalGrossApproved { get; private set; }
    public decimal TotalNetApproved { get; private set; }
    public int EmployeeCount { get; private set; }

    private PayrollApproval() { }

    public static PayrollApproval Create(
        string tenantId,
        Guid payrollRunId,
        string approvedBy,
        decimal totalGross,
        decimal totalNet,
        int employeeCount,
        string? comments = null)
    {
        return new PayrollApproval
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PayrollRunId = payrollRunId,
            ApprovedBy = approvedBy,
            ApprovedAt = DateTimeOffset.UtcNow,
            TotalGrossApproved = totalGross,
            TotalNetApproved = totalNet,
            EmployeeCount = employeeCount,
            Comments = comments
        };
    }
}
