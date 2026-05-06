namespace Karamchari.TimeAttendance.Consumers;

using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.TimeAttendance.Services;
using MassTransit;

/// <summary>
/// Consumes the decoupled LiveAttendanceUpdatedEventV1 and broadcasts it to SignalR.
/// Ensures ingestion (PunchTranslation) isn't slowed down by SignalR network latency.
/// </summary>
public sealed class LiveAttendanceConsumer : IConsumer<LiveAttendanceUpdatedEventV1>
{
    private readonly LiveAttendanceService _liveAttendance;

    public LiveAttendanceConsumer(LiveAttendanceService liveAttendance)
    {
        _liveAttendance = liveAttendance;
    }

    public async Task Consume(ConsumeContext<LiveAttendanceUpdatedEventV1> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var msg = context.Message;
        
        // Pass a mapped DTO equivalent if necessary, or just broadcast the raw event payload.
        await _liveAttendance.BroadcastRawAsync(new
        {
            msg.TenantId,
            msg.EmployeeId,
            msg.IsPresent,
            msg.Status,
            msg.LastCheckIn,
            msg.LastCheckOut,
            msg.LastUpdatedAtUtc
        }, context.CancellationToken);
    }
}
