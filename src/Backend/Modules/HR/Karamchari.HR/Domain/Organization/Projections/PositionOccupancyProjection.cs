using System;

namespace Karamchari.HR.Domain.Organization.Projections;

public sealed class PositionOccupancyProjection
{
    public string TenantId { get; set; } = string.Empty;
    public Guid PositionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid? OrgUnitId { get; set; }
    public Guid DesignationId { get; set; }
    public string DesignationName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool AllowMultipleOccupants { get; set; }
    public int ApprovedHeadcount { get; set; }
    public int CurrentOccupantCount { get; set; }
    public int VacancyCount { get; set; }
    public double OccupancyRate { get; set; }
    public DateTimeOffset LastUpdatedAtUtc { get; set; }
}
