namespace Karamchari.TimeAttendance.Persistence;

using Karamchari.Core.Multitenancy;

/// <summary>
/// Configurable blackout window during which leave applications are blocked.
/// Examples: release week, quarter close, client go-live, peak season.
/// </summary>
public sealed class LeaveBlackoutPeriod : ITenantOwned
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = string.Empty;
    public required string Name { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }

    /// <summary>Null = applies to all leave types. Set to restrict to specific types.</summary>
    public Guid? LeaveTypeId { get; init; }

    /// <summary>Null = applies org-wide. Set to restrict to specific department.</summary>
    public Guid? DepartmentId { get; init; }

    public bool IsActive { get; set; } = true;
}
