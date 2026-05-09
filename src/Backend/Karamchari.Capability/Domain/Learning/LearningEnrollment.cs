using Karamchari.Capability.Domain.Primitives;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Capability.Domain.Learning;

/// <summary>
/// Aggregate root orchestrating an employee's progression through a learning module.
/// Retry-safe and capable of handling out-of-band webhook completions.
/// </summary>
public sealed class LearningEnrollment : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public Guid ModuleId { get; private set; }
    
    public EnrollmentStatus Status { get; private set; }
    public bool IsMandatory { get; private set; }
    public DateTimeOffset? DeadlineUtc { get; private set; }
    
    public DateTimeOffset EnrolledAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public string? CompletionIdempotencyKey { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private LearningEnrollment() { }

    public static LearningEnrollment Assign(
        string tenantId, 
        Guid employeeId, 
        Guid moduleId, 
        bool isMandatory, 
        DateTimeOffset? deadline = null)
    {
        return new LearningEnrollment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            ModuleId = moduleId,
            IsMandatory = isMandatory,
            DeadlineUtc = deadline,
            Status = EnrollmentStatus.Assigned,
            EnrolledAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void Start()
    {
        if (Status != EnrollmentStatus.Assigned)
            throw new InvalidOperationException($"Cannot start enrollment from state {Status}.");

        Status = EnrollmentStatus.InProgress;
    }

    public void MarkCompleted(string idempotencyKey)
    {
        // Safe retry guard
        if (Status == EnrollmentStatus.Completed && CompletionIdempotencyKey == idempotencyKey)
            return;

        if (Status != EnrollmentStatus.Assigned && Status != EnrollmentStatus.InProgress)
            throw new InvalidOperationException($"Cannot complete enrollment from state {Status}.");

        Status = EnrollmentStatus.Completed;
        CompletedAtUtc = DateTimeOffset.UtcNow;
        CompletionIdempotencyKey = idempotencyKey;
        // Raise LearningCompletedEvent for Capability Gap recalculation
    }
}
