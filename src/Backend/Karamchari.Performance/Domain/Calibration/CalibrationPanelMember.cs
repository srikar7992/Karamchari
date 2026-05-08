using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Calibration;

/// <summary>
/// One participant in a CalibrationSession with their designated role.
/// </summary>
public sealed class CalibrationPanelMember : Entity<Guid>
{
    private CalibrationPanelMember() { /* EF materialization */ }

    internal CalibrationPanelMember(Guid id, Guid sessionId, Guid employeeId, CalibrationRole role) : base(id)
    {
        SessionId = sessionId;
        EmployeeId = employeeId;
        Role = role;
    }

    public Guid SessionId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public CalibrationRole Role { get; private set; }
}
