namespace Karamchari.TimeAttendance.Consumers;

using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.TimeAttendance.Services;
using MassTransit;

/// <summary>
/// Consumes the ReprocessAttendanceCommandV1, typically emitted by the DLQ Mapping 
/// or Bulk Reprocessing API via the Outbox pattern.
/// </summary>
public sealed class ReprocessAttendanceConsumer : IConsumer<ReprocessAttendanceCommandV1>
{
    private readonly ReprocessingWorker _worker;

    public ReprocessAttendanceConsumer(ReprocessingWorker worker)
    {
        _worker = worker;
    }

    public async Task Consume(ConsumeContext<ReprocessAttendanceCommandV1> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var msg = context.Message;
        
        // This process is idempotent. If it fails, MassTransit will retry.
        await _worker.ReprocessAsync(msg.TenantId, msg.EmployeeId, msg.FromDateUtc, msg.ToDateUtc, context.CancellationToken);
    }
}
