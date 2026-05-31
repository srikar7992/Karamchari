using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Attendance;

/// <summary>
/// Finalized operational truth for an employee's attendance on a specific date.
/// Derived from raw AttendanceEvents via the reconciliation engine.
/// Payroll-safe and immutable once finalized.
/// </summary>
public sealed class AttendanceRecord : AggregateRoot<Guid>, ITenantOwned
{
    private AttendanceRecord() { }

    private AttendanceRecord(
        Guid id,
        string tenantId,
        Guid employeeId,
        DateOnly date,
        Guid? shiftId,
        AttendanceStatus status,
        decimal workedHours,
        decimal overtimeHours,
        bool isRegularized) : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        Date = date;
        ShiftId = shiftId;
        Status = status;
        WorkedHours = workedHours;
        OvertimeHours = overtimeHours;
        IsRegularized = isRegularized;
        FinalizedAtUtc = DateTimeOffset.UtcNow;
    }

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public DateOnly Date { get; private set; }
    public Guid? ShiftId { get; private set; }

    public AttendanceStatus Status { get; private set; }

    public decimal WorkedHours { get; private set; }
    public decimal OvertimeHours { get; private set; }

    public bool IsRegularized { get; private set; }
    public DateTimeOffset FinalizedAtUtc { get; private set; }

    public static AttendanceRecord Finalize(
        string tenantId,
        Guid employeeId,
        DateOnly date,
        Guid? shiftId,
        AttendanceStatus status,
        decimal workedHours,
        decimal overtimeHours,
        bool isRegularized = false)
    {
        return new AttendanceRecord(
            Guid.NewGuid(), tenantId, employeeId, date,
            shiftId, status, workedHours, overtimeHours, isRegularized);
    }
}
