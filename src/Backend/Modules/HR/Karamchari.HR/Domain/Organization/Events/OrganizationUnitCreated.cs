using System;
using Karamchari.Core.Domain.Primitives;

namespace Karamchari.HR.Domain.Organization.Events;

public sealed record OrganizationUnitCreated(
    Guid UnitId,
    string TenantId,
    string Name,
    string Code,
    OrganizationUnitType Type,
    Guid? ParentUnitId,
    Guid? LeadEmployeeId,
    Guid? CostCenterId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
