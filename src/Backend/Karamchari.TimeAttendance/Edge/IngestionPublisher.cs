namespace Karamchari.TimeAttendance.Edge;

using Karamchari.Core.Contracts.IntegrationEvents;
using MassTransit;

/// <summary>
/// Fast edge publisher. Dumps raw payload to MassTransit queue.
/// Does no parsing or business logic.
/// </summary>
public sealed class IngestionPublisher
{
    private readonly IPublishEndpoint _bus;

    public IngestionPublisher(IPublishEndpoint bus)
    {
        _bus = bus;
    }

    public async Task PublishAsync(string rawPayload, string deviceId, string tenantId, bool isMobile, CancellationToken ct = default)
    {
        var evt = new RawPunchReceivedEvent
        {
            Payload = rawPayload,
            DeviceId = deviceId,
            TenantId = tenantId,
            ReceivedAtUtc = DateTime.UtcNow,
            IsMobileSource = isMobile
        };

        // Fire and forget approach could be used for extreme throughput,
        // but awaiting ensures it's safely enqueued to RabbitMQ/Kafka.
        await _bus.Publish(evt, ct);
    }
}
