using System;

namespace Karamchari.HR.Domain.Organization.Projections;

public sealed class ManagerDirectReportProjection
{
    public string TenantId { get; set; } = string.Empty;
    public Guid ManagerPositionId { get; set; }
    public Guid DirectReportPositionId { get; set; }
    public Guid? DirectReportEmployeeId { get; set; }
    public string? DirectReportEmployeeName { get; set; }
}
