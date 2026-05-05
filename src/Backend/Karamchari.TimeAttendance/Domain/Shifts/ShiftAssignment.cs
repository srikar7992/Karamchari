namespace Karamchari.TimeAttendance.Domain.Shifts;

using Karamchari.Core.Multitenancy;

public sealed class ShiftAssignment : ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;

    public Guid Id { get; private set; }

    public Guid EmployeeId { get; private set; }

    public Guid ShiftTemplateId { get; private set; }

    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }

    /// <summary>If true, the ShiftTemplateId points to a rotation pattern root instead of a direct shift.</summary>
    public bool IsRotation { get; private set; }

    /// <summary>Length of the rotation cycle in days (e.g. 7, 14).</summary>
    public int RotationCycleDays { get; private set; }

    private ShiftAssignment() { }

    public static ShiftAssignment Create(
        string tenantId,
        Guid employeeId,
        Guid shiftTemplateId,
        DateTime effectiveFrom,
        bool isRotation = false,
        int rotationCycleDays = 0)
    {
        return new ShiftAssignment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            ShiftTemplateId = shiftTemplateId,
            EffectiveFrom = effectiveFrom,
            IsRotation = isRotation,
            RotationCycleDays = rotationCycleDays
        };
    }
}
