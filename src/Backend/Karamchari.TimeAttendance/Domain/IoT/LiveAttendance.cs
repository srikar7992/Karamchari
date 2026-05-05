namespace Karamchari.TimeAttendance.Domain.IoT;

using Karamchari.Core.Multitenancy;

/// <summary>
/// Real-time live attendance state for an employee. Updated synchronously by the PunchTranslationConsumer.
/// </summary>
public sealed class LiveAttendance : ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;

    public Guid Id { get; private set; }

    public Guid EmployeeId { get; private set; }

    public DateTime? LastCheckIn { get; private set; }
    public DateTime? LastCheckOut { get; private set; }

    public bool IsPresent { get; private set; }

    /// <summary>E.g., "Present", "Late", "Absent", "OnBreak", "Out"</summary>
    public string Status { get; private set; } = string.Empty;

    public DateTime LastUpdatedAtUtc { get; private set; }

    private LiveAttendance() { }

    public static LiveAttendance Create(string tenantId, Guid employeeId)
    {
        return new LiveAttendance
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            IsPresent = false,
            Status = "Absent",
            LastUpdatedAtUtc = DateTime.UtcNow
        };
    }

    public void UpdateCheckIn(DateTime timestamp, bool isLate)
    {
        LastCheckIn = timestamp;
        IsPresent = true;
        Status = isLate ? "Late" : "Present";
        LastUpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateCheckOut(DateTime timestamp)
    {
        LastCheckOut = timestamp;
        IsPresent = false;
        Status = "Out";
        LastUpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAbsent()
    {
        IsPresent = false;
        Status = "Absent";
        LastUpdatedAtUtc = DateTime.UtcNow;
    }
}
