using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Geofencing;

/// <summary>
/// Assigns an employee to a specific geographical boundary.
/// An employee can be assigned to multiple boundaries (e.g., roaming field agents).
/// </summary>
public sealed class EmployeeLocationAssignment : ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;

    public Guid EmployeeId { get; private set; }

    public Guid LocationBoundaryId { get; private set; }

    private EmployeeLocationAssignment() { }

    public static EmployeeLocationAssignment Create(string tenantId, Guid employeeId, Guid locationBoundaryId)
    {
        return new EmployeeLocationAssignment
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            LocationBoundaryId = locationBoundaryId
        };
    }
}
