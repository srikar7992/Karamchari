using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Rostering;

public sealed class ScheduleAlert : Entity<Guid>, ITenantOwned
{
    private ScheduleAlert() { TenantId = string.Empty; }

    public string TenantId { get; private set; }
    public Guid RosterId { get; private set; }
    public ExceptionType Type { get; private set; }
    public ExceptionSeverity Severity { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid? ShiftAssignmentId { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public bool IsResolved { get; private set; }
    public DateTimeOffset DetectedAtUtc { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }

    public static ScheduleAlert Detect(
        string tenantId,
        Guid rosterId,
        Guid employeeId,
        Guid? shiftAssignmentId,
        ExceptionType type,
        ExceptionSeverity severity,
        string message)
    {
        return new ScheduleAlert
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RosterId = rosterId,
            EmployeeId = employeeId,
            ShiftAssignmentId = shiftAssignmentId,
            Type = type,
            Severity = severity,
            Message = message,
            DetectedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Resolve()
    {
        IsResolved = true;
        ResolvedAtUtc = DateTimeOffset.UtcNow;
    }
}
