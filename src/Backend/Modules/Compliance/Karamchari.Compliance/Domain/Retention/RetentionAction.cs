// -----------------------------------------------------------------------
// <copyright file="RetentionAction.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Compliance.Domain.Retention;

/// <summary>
/// A scheduled or executed retention action against a specific record.
/// Append-only audit trail of every archive/delete/anonymize operation.
/// </summary>
public sealed class RetentionAction : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public RetentionRecordType RecordType { get; private set; }
    public Guid RecordId { get; private set; }
    public RetentionActionType Action { get; private set; }
    public RetentionActionStatus Status { get; private set; }
    public DateTime ScheduledFor { get; private set; }
    public DateTime? ExecutedAt { get; private set; }
    public string? FailureReason { get; private set; }

    private RetentionAction() { }

    public static RetentionAction Schedule(
        string tenantId,
        RetentionRecordType recordType,
        Guid recordId,
        RetentionActionType action,
        DateTime scheduledFor)
    {
        return new RetentionAction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RecordType = recordType,
            RecordId = recordId,
            Action = action,
            Status = RetentionActionStatus.Scheduled,
            ScheduledFor = scheduledFor
        };
    }

    public void MarkCompleted()
    {
        Status = RetentionActionStatus.Completed;
        ExecutedAt = DateTime.UtcNow;
    }

    public void MarkSkipped(string reason)
    {
        Status = RetentionActionStatus.Skipped;
        FailureReason = reason;
        ExecutedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        Status = RetentionActionStatus.Failed;
        FailureReason = reason;
        ExecutedAt = DateTime.UtcNow;
    }
}
