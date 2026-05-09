namespace Karamchari.Core.Messaging.Sagas;

/// <summary>
/// Standard timeout event for workflows.
/// Sent by MassTransit scheduling when a state does not transition within the SLA.
/// </summary>
public sealed record SagaTimeoutEvent
{
    public Guid CorrelationId { get; init; }
    public string StateName { get; init; } = string.Empty;
    public DateTimeOffset TimeoutAtUtc { get; init; }
}
