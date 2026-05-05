namespace Karamchari.TimeAttendance.Domain.IoT;

using Karamchari.Core.Multitenancy;

/// <summary>
/// Audit trail for any changes made to an AttendanceResult (e.g. manual HR override, or engine reprocessing).
/// </summary>
public sealed class AttendanceAudit : ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;

    public Guid Id { get; private set; }

    public Guid EmployeeId { get; private set; }

    public DateTime Date { get; private set; }

    public string OldValue { get; private set; } = string.Empty;
    public string NewValue { get; private set; } = string.Empty;

    public string Reason { get; private set; } = string.Empty;

    public DateTime ChangedAtUtc { get; private set; }

    private AttendanceAudit() { }

    public static AttendanceAudit Create(
        string tenantId,
        Guid employeeId,
        DateTime date,
        string oldValue,
        string newValue,
        string reason)
    {
        return new AttendanceAudit
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            Date = date,
            OldValue = oldValue,
            NewValue = newValue,
            Reason = reason,
            ChangedAtUtc = DateTime.UtcNow
        };
    }
}
