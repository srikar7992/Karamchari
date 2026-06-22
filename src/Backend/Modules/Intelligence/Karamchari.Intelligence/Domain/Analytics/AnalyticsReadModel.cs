namespace Karamchari.Intelligence.Domain.Analytics;

/// <summary>
/// Denormalized read model that captures a single analytics measurement for a tenant.
/// One row is materialized per event consumed by an analytics consumer.
/// </summary>
public sealed class AnalyticsReadModel
{
    /// <summary>
    /// Unique identifier for this analytics record.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Tenant that owns this analytics record.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>
    /// Discriminator that categorizes the metric (e.g. "RecruitmentVelocity", "TimeToHire", "HiringFunnel").
    /// </summary>
    public string MetricType { get; init; } = string.Empty;

    /// <summary>
    /// Primary entity identifier relevant to this metric (RequisitionId, ApplicationId, etc.).
    /// </summary>
    public Guid EntityId { get; init; }

    /// <summary>
    /// Stage name within the hiring pipeline (e.g. "Published", "Applied", "InterviewCompleted", "OfferAccepted", "Hired").
    /// </summary>
    public string Stage { get; init; } = string.Empty;

    /// <summary>
    /// Numeric value associated with this measurement.
    /// For velocity metrics this is elapsed days between stages.
    /// For funnel metrics this is 1.0 (a count increment per event).
    /// </summary>
    public decimal Value { get; init; }

    /// <summary>
    /// UTC timestamp from the originating integration event.
    /// </summary>
    public DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// UTC timestamp when this read model row was written.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}
