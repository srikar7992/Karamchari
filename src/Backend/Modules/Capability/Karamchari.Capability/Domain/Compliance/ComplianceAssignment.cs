using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Capability.Domain.Compliance;

public sealed class ComplianceAssignment : AggregateRoot<Guid>, ITenantOwned
{
    private ComplianceAssignment() { }

    public string TenantId { get; private set; } = string.Empty;
    public Guid ModuleId { get; private set; }
    public string TargetGroup { get; private set; } = string.Empty;
    public DateOnly DueDate { get; private set; }
    public ComplianceRecurrenceType Recurrence { get; private set; }
    public ComplianceAssignmentStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static ComplianceAssignment Create(
        string tenantId, Guid moduleId, string targetGroup,
        DateOnly dueDate, ComplianceRecurrenceType recurrence)
        => new()
        {
            Id = Guid.NewGuid(), TenantId = tenantId, ModuleId = moduleId,
            TargetGroup = targetGroup, DueDate = dueDate, Recurrence = recurrence,
            Status = ComplianceAssignmentStatus.Active, CreatedAtUtc = DateTimeOffset.UtcNow
        };

    public void Close() => Status = ComplianceAssignmentStatus.Closed;
}
