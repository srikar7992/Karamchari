namespace Karamchari.TimeAttendance.Domain.Shifts;

using Karamchari.Core.Multitenancy;

public sealed class WeeklyOffRule : ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;

    public Guid Id { get; private set; }

    public Guid EmployeeId { get; private set; }

    public DayOfWeek Day { get; private set; }

    /// <summary>E.g., Alternate Saturday Off.</summary>
    public bool IsAlternateWeek { get; private set; }

    private WeeklyOffRule() { }

    public static WeeklyOffRule Create(string tenantId, Guid employeeId, DayOfWeek day, bool isAlternateWeek)
    {
        return new WeeklyOffRule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            Day = day,
            IsAlternateWeek = isAlternateWeek
        };
    }
}
