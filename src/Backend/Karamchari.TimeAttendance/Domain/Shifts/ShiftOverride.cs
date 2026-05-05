namespace Karamchari.TimeAttendance.Domain.Shifts;

using Karamchari.Core.Multitenancy;

public sealed class ShiftOverride : ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;

    public Guid Id { get; private set; }

    public Guid EmployeeId { get; private set; }

    public DateTime Date { get; private set; }

    public Guid ShiftTemplateId { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    private ShiftOverride() { }

    public static ShiftOverride Create(string tenantId, Guid employeeId, DateTime date, Guid shiftTemplateId, string reason)
    {
        return new ShiftOverride
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            Date = date,
            ShiftTemplateId = shiftTemplateId,
            Reason = reason
        };
    }
}
