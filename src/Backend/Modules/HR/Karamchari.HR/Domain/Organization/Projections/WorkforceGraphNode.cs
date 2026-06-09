using System;

namespace Karamchari.HR.Domain.Organization.Projections;

public sealed class WorkforceGraphNode
{
    public string TenantId { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public WorkforceNodeType Type { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? PropertiesJson { get; set; }
}
