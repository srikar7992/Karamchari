namespace Karamchari.Billing.Domain.Analytics;

/// <summary>
/// Tracks processed event IDs per consumer to ensure exact-once processing.
/// </summary>
public sealed class ProcessedEventLog
{
    public Guid EventId { get; init; }
    public string ConsumerName { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; init; } = DateTimeOffset.UtcNow;
}
