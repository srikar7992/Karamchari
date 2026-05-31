namespace Karamchari.Intelligence.Domain.Projections;

/// <summary>
/// Represents a denormalized read model for cross-domain intelligence queries.
/// Protects operational OLTP databases from heavy analytical loads.
/// </summary>
public abstract class AnalyticsProjectionBase
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid Id { get; protected set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; protected set; } = string.Empty;

    /// <summary>
    /// Tracks the occurrence time of the last event that modified this projection.
    /// Critical for detecting stale projections and ensuring deterministic replays.
    /// </summary>
    public DateTimeOffset LastProcessedOccurredAtUtc { get; protected set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset LastUpdatedAtUtc { get; protected set; }

    /// <inheritdoc/>
    protected void MarkProcessed(DateTimeOffset eventOccurredAtUtc)
    {
        // Out-of-order event protection for eventual consistency
        if (eventOccurredAtUtc > LastProcessedOccurredAtUtc)
        {
            LastProcessedOccurredAtUtc = eventOccurredAtUtc;
        }
        LastUpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
