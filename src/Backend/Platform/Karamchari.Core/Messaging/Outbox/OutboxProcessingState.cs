namespace Karamchari.Core.Messaging.Outbox;

/// <summary>
/// Relay-owned processing state for each <c>dbo.OutboxState</c> row.
/// Persists retry counts and lock-acquisition metadata so relay restarts
/// do not reset progress and stale locks are detected by wall-clock time,
/// not by the age of the originating OutboxState row.
/// Lives in <c>dbo.OutboxProcessingState</c> â€” shared infrastructure, not tenant-owned.
/// </summary>
public sealed class OutboxProcessingState
{
    /// <summary>OutboxId from <c>dbo.OutboxState</c>. Primary key and logical FK.</summary>
    public Guid OutboxId { get; set; }

    /// <summary>Number of publish attempts made so far across all relay instances and restarts.</summary>
    public int RetryCount { get; set; }

    /// <summary>UTC wall-clock time of the last publish attempt, if any.</summary>
    public DateTime? LastAttemptUtc { get; set; }

    /// <summary>
    /// UTC time before which this OutboxId must NOT be retried.
    /// <c>null</c> means eligible immediately. Set on each transient failure with exponential backoff.
    /// </summary>
    public DateTime? NextRetryUtc { get; set; }

    /// <summary>
    /// Truncated exception message from the last failed publish attempt.
    /// Stored for dead-letter diagnostics and operational debugging.
    /// Max 2000 characters.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Instance ID of the relay that currently holds <c>dbo.OutboxState.LockId</c>.
    /// <c>null</c> when unlocked. Set atomically with <see cref="LockAcquiredUtc"/>.
    /// </summary>
    public Guid? LockedByInstanceId { get; set; }

    /// <summary>
    /// UTC wall-clock time when the current lock was acquired.
    /// Used for stale-lock detection: rows with <c>LockAcquiredUtc &lt; NOW() âˆ’ StaleLockTimeout</c>
    /// are considered abandoned by a dead relay instance and released on startup.
    /// </summary>
    public DateTime? LockAcquiredUtc { get; set; }
}
