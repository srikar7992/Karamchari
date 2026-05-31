using Karamchari.Core.Multitenancy;

namespace Karamchari.DataMigration.Domain.Importing;

/// <summary>
/// Stores an imported salary component allocation for an employee.
/// Used as a staging area for historical salary structure data before live domain reconciliation.
/// </summary>
public sealed class HistoricalSalaryRecord : ITenantOwned
{
    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public Guid ImportJobId { get; private set; }
    public string EmployeeNumber { get; private set; } = string.Empty;
    public string ComponentName { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Frequency { get; private set; } = string.Empty;
    public DateTime ImportedAt { get; private set; }

    private HistoricalSalaryRecord() { }

    public static HistoricalSalaryRecord Create(
        Guid importJobId,
        string employeeNumber,
        string componentName,
        decimal amount,
        string frequency)
    {
        return new HistoricalSalaryRecord
        {
            Id = Guid.NewGuid(),
            ImportJobId = importJobId,
            EmployeeNumber = employeeNumber,
            ComponentName = componentName,
            Amount = amount,
            Frequency = frequency,
            ImportedAt = DateTime.UtcNow
        };
    }
}
