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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid SessionId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EmployeeId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public CalibrationRole Role { get; private set; }
}
