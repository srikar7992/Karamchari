using Karamchari.Approvals.Domain;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Karamchari.Approvals.Consumers;

public sealed class ApprovalActionedNotificationConsumer(ILogger<ApprovalActionedNotificationConsumer> logger)
    : IConsumer<ApprovalRequestActioned>
{
    public Task Consume(ConsumeContext<ApprovalRequestActioned> context)
    {
        var msg = context.Message;
        var outcome = msg.Decision == ApprovalRequestStatus.Approved ? "approved" : "rejected";
        logger.LogInformation(
            "ApprovalRequest {RequestId} {Outcome} by {ActorId} in tenant {TenantId}",
            msg.RequestId, outcome, msg.ActorId, msg.TenantId);
        return Task.CompletedTask;
    }
}
