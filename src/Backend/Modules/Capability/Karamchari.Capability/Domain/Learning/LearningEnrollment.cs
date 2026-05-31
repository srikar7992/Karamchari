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
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EmployeeId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ModuleId { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public EnrollmentStatus Status { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsMandatory { get; private set; }
    /// <inheritdoc/>
    public DateTimeOffset? DeadlineUtc { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset EnrolledAtUtc { get; private set; }
    /// <inheritdoc/>
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    /// <inheritdoc/>
    public string? CompletionIdempotencyKey { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    private LearningEnrollment() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Start()
    {
        if (Status != EnrollmentStatus.Assigned)
            throw new InvalidOperationException($"Cannot start enrollment from state {Status}.");

        Status = EnrollmentStatus.InProgress;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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
