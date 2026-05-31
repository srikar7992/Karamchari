using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Analytics;

/// <summary>
/// Idempotency log for analytics consumers.
/// Prevents double-counting when MassTransit redelivers at-least-once messages.
/// </summary>
public sealed class ProcessedEventLog : ITenantOwned
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>The EventId from the integration event base record.</summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string ConsumerName { get; set; } = string.Empty;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset ProcessedAt { get; set; }
}
