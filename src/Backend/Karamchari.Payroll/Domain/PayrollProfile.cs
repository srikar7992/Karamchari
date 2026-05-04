namespace Karamchari.Payroll.Domain;

/// <summary>
/// TODO: Add XML documentation.
/// </summary>
public class PayrollProfile
{
    /// <summary>
    /// TODO: Add XML documentation.
    /// </summary>
    public Guid Id { get; private set; }
    /// <summary>
    /// TODO: Add XML documentation.
    /// </summary>
    public Guid EmployeeId { get; private set; } // Soft reference to HR Domain
    /// <summary>
    /// TODO: Add XML documentation.
    /// </summary>
    public decimal BaseSalary { get; private set; }
    /// <summary>
    /// TODO: Add XML documentation.
    /// </summary>
    public string Currency { get; private set; }
    /// <summary>
    /// TODO: Add XML documentation.
    /// </summary>
    public bool IsActive { get; private set; }

    private PayrollProfile()
    {
        Currency = string.Empty;
    } // EF Core

    /// <summary>
    /// TODO: Add XML documentation.
    /// </summary>
    public static PayrollProfile CreateDraft(Guid employeeId)
    {
        return new PayrollProfile
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            BaseSalary = 0.00m, // Requires HR/Admin to finalize later
            Currency = "USD", 
            IsActive = false
        };
    }
}
