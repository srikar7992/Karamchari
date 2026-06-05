using Karamchari.Core.Multitenancy;

namespace Karamchari.Payroll.Domain.Calculation;

/// <summary>
/// Per-employee earnings line item for a payroll run. Immutable financial record.
/// </summary>
public sealed class PayrollEarning : ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public Guid Id { get; private set; }
    public Guid PayrollRunId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public EarningType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private PayrollEarning() { }

    public static PayrollEarning Create(
        string tenantId,
        Guid payrollRunId,
        Guid employeeId,
        EarningType type,
        decimal amount,
        string description = "")
    {
        if (amount < 0)
            throw new ArgumentException("Earning amount must not be negative.");

        return new PayrollEarning
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PayrollRunId = payrollRunId,
            EmployeeId = employeeId,
            Type = type,
            Amount = amount,
            Description = description,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
