using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Core.Domain.Workflows;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.TimeAttendance.Consumers;

/// <summary>
/// Reacts to workflow completion to update authoritative business state.
/// Implements "Domain Owns Business Truth, Workflow Owns Coordination".
/// </summary>
public sealed class WorkflowCompletedConsumer :
    IConsumer<WorkflowCompletedIntegrationEvent>
{
    private readonly TimeAttendanceDbContext _db;
    private readonly ILogger<WorkflowCompletedConsumer> _logger;

    public WorkflowCompletedConsumer(TimeAttendanceDbContext db, ILogger<WorkflowCompletedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<WorkflowCompletedIntegrationEvent> context)
    {
        var msg = context.Message;

        if (msg.EntityType == "LeaveRequest")
        {
            await HandleLeaveRequestCompletion(msg, context.CancellationToken);
        }
        // Add other entities as they are migrated to the new flow
    }

    private async Task HandleLeaveRequestCompletion(WorkflowCompletedIntegrationEvent msg, CancellationToken ct)
    {
        var leave = await _db.Set<LeaveRequest>().FindAsync([msg.EntityId], ct);
        if (leave == null)
        {
            _logger.LogWarning("Leave Request {RequestId} not found for completion.", msg.EntityId);
            return;
        }

        _logger.LogInformation("Finalizing Leave Request {RequestId} with status {Status}.", msg.EntityId, msg.FinalStatus);

        if (msg.FinalStatus == WorkflowStatus.Approved.ToString())
        {
            leave.FinalizeApproved();
        }
        else if (msg.FinalStatus == WorkflowStatus.Rejected.ToString())
        {
            leave.FinalizeRejected();
        }

        await _db.SaveChangesAsync(ct);
    }
}
