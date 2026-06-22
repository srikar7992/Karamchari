using Karamchari.Capability.Domain.Events;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Capability.Domain.LearningPaths;

public sealed class LearningPathEnrollment : AggregateRoot<Guid>, ITenantOwned
{
    private LearningPathEnrollment() { }

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public Guid PathId { get; private set; }
    public int ProgressPercent { get; private set; }
    public DateTimeOffset EnrolledAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public static LearningPathEnrollment Enroll(string tenantId, Guid employeeId, Guid pathId)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            PathId = pathId,
            EnrolledAtUtc = DateTimeOffset.UtcNow
        };

    public void UpdateProgress(int percent)
    {
        ProgressPercent = Math.Clamp(percent, 0, 100);
        if (ProgressPercent == 100 && CompletedAtUtc is null)
        {
            CompletedAtUtc = DateTimeOffset.UtcNow;
            RaiseDomainEvent(new LearningPathCompleted(Id, TenantId, EmployeeId, PathId));
        }
    }
}
