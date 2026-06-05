using Karamchari.Core.Multitenancy;

namespace Karamchari.Payroll.Domain.Compensation;

/// <summary>
/// Versioned employee compensation record. Never update — insert new version.
/// Effective-date history enables deterministic retroactive calculations.
/// </summary>
public sealed class CompensationProfile : ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public decimal HourlyRate { get; private set; }
    public decimal MonthlySalary { get; private set; }
    public decimal OvertimeMultiplier { get; private set; }
    public string Currency { get; private set; } = "INR";
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private CompensationProfile() { }

    public static CompensationProfile Create(
        string tenantId,
        Guid employeeId,
        decimal monthlySalary,
        decimal hourlyRate,
        decimal overtimeMultiplier,
        string currency,
        DateOnly effectiveFrom)
    {
        if (monthlySalary < 0 || hourlyRate < 0)
            throw new ArgumentException("Salary and rate must not be negative.");

        return new CompensationProfile
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            MonthlySalary = monthlySalary,
            HourlyRate = hourlyRate,
            OvertimeMultiplier = overtimeMultiplier <= 0 ? 1.5m : overtimeMultiplier,
            Currency = currency,
            EffectiveFrom = effectiveFrom,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Supersedes this version. Call before inserting a new version.</summary>
    public void Supersede(DateOnly effectiveTo)
    {
        if (!IsActive)
            throw new InvalidOperationException("Profile already superseded.");

        EffectiveTo = effectiveTo;
        IsActive = false;
    }
}
