namespace Karamchari.TimeAttendance.Domain.IoT;

using Karamchari.Core.Multitenancy;

/// <summary>
/// The final daily outcome of the shift reconciliation engine.
/// This feeds directly into TimeEntry / Timesheet.
/// </summary>
public sealed class AttendanceResult : ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;

    public Guid Id { get; private set; }

    public Guid EmployeeId { get; private set; }

    public DateTime Date { get; private set; }

    public bool IsPresent { get; private set; }
    public bool IsLate { get; private set; }
    public bool IsHalfDay { get; private set; }
    public bool IsAbsent { get; private set; }

    public int WorkedMinutes { get; private set; }
    public int OvertimeMinutes { get; private set; }

    public string Notes { get; private set; } = string.Empty;

    /// <summary>Version of the reconciliation engine that generated this result.</summary>
    public string GeneratedByVersion { get; private set; } = string.Empty;

    private AttendanceResult() { }

    public static AttendanceResult Create(
        string tenantId,
        Guid employeeId,
        DateTime date,
        bool isPresent,
        bool isLate,
        bool isHalfDay,
        bool isAbsent,
        int workedMinutes,
        int overtimeMinutes,
        string notes,
        string version)
    {
        return new AttendanceResult
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            Date = date,
            IsPresent = isPresent,
            IsLate = isLate,
            IsHalfDay = isHalfDay,
            IsAbsent = isAbsent,
            WorkedMinutes = workedMinutes,
            OvertimeMinutes = overtimeMinutes,
            Notes = notes,
            GeneratedByVersion = version
        };
    }

    /// <summary>
    /// Updates the existing result (for idempotent replay/reprocessing).
    /// </summary>
    public void Update(
        bool isPresent,
        bool isLate,
        bool isHalfDay,
        bool isAbsent,
        int workedMinutes,
        int overtimeMinutes,
        string notes,
        string version)
    {
        IsPresent = isPresent;
        IsLate = isLate;
        IsHalfDay = isHalfDay;
        IsAbsent = isAbsent;
        WorkedMinutes = workedMinutes;
        OvertimeMinutes = overtimeMinutes;
        Notes = notes;
        GeneratedByVersion = version;
    }
}
