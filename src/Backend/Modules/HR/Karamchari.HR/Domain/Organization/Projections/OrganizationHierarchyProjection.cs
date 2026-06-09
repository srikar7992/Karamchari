using System;

namespace Karamchari.HR.Domain.Organization.Projections;

public sealed class OrganizationHierarchyProjection
{
    public string TenantId { get; set; } = string.Empty;
    public Guid OrganizationUnitId { get; set; }
    public Guid? ParentUnitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string HierarchyPath { get; set; } = string.Empty;
    public int Level { get; set; }
    public DateTimeOffset LastUpdatedAtUtc { get; set; }
}
