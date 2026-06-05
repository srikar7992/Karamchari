using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Persistence;

public enum StatutoryRegisterType
{
    LeaveRegister = 1,
    MaternityRegister = 2,
    CompOffRegister = 3,
    PaternityRegister = 4,
    BereavementRegister = 5,
}

public enum StatutoryRegisterStatus
{
    Active = 1,
    Exported = 2,
    Archived = 3,
}

/// <summary>
/// Statutory register entry for compliance reporting required by India, Middle East, and Southeast Asia labour laws.
/// Exportable to PDF/Excel. One row per leave event per employee. Append-only.
/// </summary>
public sealed class StatutoryRegisterEntry : ITenantOwned
{
    private StatutoryRegisterEntry() { TenantId = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public StatutoryRegisterType RegisterType { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string EmployeeCode { get; private set; } = string.Empty;
    public string EmployeeName { get; private set; } = string.Empty;
    public string Department { get; private set; } = string.Empty;
    public string Designation { get; private set; } = string.Empty;
    public Guid LeaveRequestId { get; private set; }
    public string LeaveTypeName { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public decimal Days { get; private set; }
    public string Status { get; private set; } = string.Empty;
    /// <summary>Balance before this leave event (for register audit column).</summary>
    public decimal OpeningBalance { get; private set; }
    public decimal ClosingBalance { get; private set; }
    public string? Remarks { get; private set; }
    public int CalendarYear { get; private set; }
    public StatutoryRegisterStatus EntryStatus { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }

    public static StatutoryRegisterEntry Create(
        string tenantId,
        StatutoryRegisterType registerType,
        Guid employeeId,
        string employeeCode,
        string employeeName,
        string department,
        string designation,
        Guid leaveRequestId,
        string leaveTypeName,
        DateOnly startDate,
        DateOnly endDate,
        decimal days,
        string leaveStatus,
        decimal openingBalance,
        decimal closingBalance,
        int calendarYear,
        string? remarks = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(days);

        return new StatutoryRegisterEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RegisterType = registerType,
            EmployeeId = employeeId,
            EmployeeCode = employeeCode,
            EmployeeName = employeeName,
            Department = department,
            Designation = designation,
            LeaveRequestId = leaveRequestId,
            LeaveTypeName = leaveTypeName,
            StartDate = startDate,
            EndDate = endDate,
            Days = days,
            Status = leaveStatus,
            OpeningBalance = openingBalance,
            ClosingBalance = closingBalance,
            CalendarYear = calendarYear,
            Remarks = remarks,
            EntryStatus = StatutoryRegisterStatus.Active,
            RecordedAt = DateTimeOffset.UtcNow,
        };
    }

    public void MarkExported() => EntryStatus = StatutoryRegisterStatus.Exported;
    public void Archive() => EntryStatus = StatutoryRegisterStatus.Archived;
}
