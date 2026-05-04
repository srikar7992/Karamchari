namespace Karamchari.Payroll.Domain;

using Karamchari.Core.Multitenancy;

/// <summary>
/// Defines how an employee is compensated (Fixed vs Variable).
/// </summary>
public enum PayType
{
    /// <summary>Fixed monthly salary regardless of hours worked.</summary>
    Salaried = 0,

    /// <summary>Compensation based on approved billable hours.</summary>
    Hourly = 1
}

/// <summary>
/// Domain model for an employee's payroll configuration and compensation rules.
/// </summary>
public class PayrollProfile : ITenantOwned
{
    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the unique identifier for the profile.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the employee identifier (Soft reference to HR Domain).
    /// </summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>
    /// Gets the compensation type.
    /// </summary>
    public PayType PayType { get; private set; }

    /// <summary>
    /// Gets the fixed monthly base salary. Only used if PayType is Salaried.
    /// </summary>
    public decimal BaseSalary { get; private set; }

    /// <summary>
    /// Gets the hourly rate. Only used if PayType is Hourly.
    /// </summary>
    public decimal HourlyRate { get; private set; }

    /// <summary>
    /// Gets the currency code (e.g., USD).
    /// </summary>
    public string Currency { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the profile is active.
    /// </summary>
    public bool IsActive { get; private set; }

    private PayrollProfile()
    {
        Currency = string.Empty;
    }

    /// <summary>
    /// Creates a new draft payroll profile.
    /// </summary>
    /// <param name="employeeId">The employee identifier.</param>
    /// <returns>A new <see cref="PayrollProfile"/> instance.</returns>
    public static PayrollProfile CreateDraft(Guid employeeId)
    {
        return new PayrollProfile
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            PayType = PayType.Salaried,
            BaseSalary = 0.00m,
            HourlyRate = 0.00m,
            Currency = "USD",
            IsActive = false
        };
    }

    /// <summary>
    /// Sets the compensation as salaried.
    /// </summary>
    /// <param name="baseSalary">The base salary.</param>
    public void SetSalaried(decimal baseSalary)
    {
        PayType = PayType.Salaried;
        BaseSalary = baseSalary;
        HourlyRate = 0;
    }

    /// <summary>
    /// Sets the compensation as hourly.
    /// </summary>
    /// <param name="hourlyRate">The hourly rate.</param>
    public void SetHourly(decimal hourlyRate)
    {
        PayType = PayType.Hourly;
        HourlyRate = hourlyRate;
        BaseSalary = 0;
    }

    /// <summary>
    /// Activates the profile.
    /// </summary>
    public void Activate() => IsActive = true;
}
