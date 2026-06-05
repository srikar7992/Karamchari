using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Payroll.Domain.Payslips.Events;

namespace Karamchari.Payroll.Domain.Payslips;

public enum PayslipStatus
{
    Generated = 1,
    Emailed = 2,
    Downloaded = 3
}

/// <summary>
/// Immutable financial record of an employee's pay for one payroll run.
/// Generated once after payroll is approved. Never recalculated in place.
/// </summary>
public sealed class Payslip : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public string EmployeeName { get; private set; } = string.Empty;
    public Guid PayrollRunId { get; private set; }
    /// <summary>
    /// FK to the EmployeePayrollResult that is the authoritative source of these figures.
    /// Payslip is a read-only projection — financial data flows one-way from result to payslip.
    /// Regenerating the PDF never changes this field or any financial field.
    /// </summary>
    public Guid PayrollResultId { get; private set; }
    public string PeriodName { get; private set; } = string.Empty;
    public decimal GrossPay { get; private set; }
    public decimal TotalDeductions { get; private set; }
    public decimal NetPay { get; private set; }
    public PayslipStatus Status { get; private set; }
    public string? StoragePath { get; private set; }
    public DateTimeOffset GeneratedAtUtc { get; private set; }
    public DateTimeOffset? EmailedAtUtc { get; private set; }

    private Payslip() { }

    public static Payslip Generate(
        string tenantId,
        Guid employeeId,
        string employeeName,
        Guid payrollRunId,
        Guid payrollResultId,
        string periodName,
        decimal grossPay,
        decimal totalDeductions,
        decimal netPay)
    {
        if (netPay < 0)
            throw new InvalidOperationException("Net pay must not be negative.");

        var payslip = new Payslip
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            PayrollRunId = payrollRunId,
            PayrollResultId = payrollResultId,
            PeriodName = periodName,
            GrossPay = grossPay,
            TotalDeductions = totalDeductions,
            NetPay = netPay,
            Status = PayslipStatus.Generated,
            GeneratedAtUtc = DateTimeOffset.UtcNow
        };

        payslip.RaiseDomainEvent(new PayslipGeneratedEvent(payslip.Id, tenantId, employeeId, payrollRunId, netPay));
        return payslip;
    }

    public void AttachStorage(string storagePath)
    {
        if (!string.IsNullOrWhiteSpace(StoragePath))
            throw new InvalidOperationException("Storage path already set.");

        StoragePath = storagePath;
    }

    public void MarkEmailed()
    {
        Status = PayslipStatus.Emailed;
        EmailedAtUtc = DateTimeOffset.UtcNow;
    }
}
