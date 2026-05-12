using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Core.Domain.Workflows;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.Reimbursements;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Payroll.Consumers;

/// <summary>
/// Reacts to workflow completion to update authoritative business state for Payroll entities.
/// Implements "Domain Owns Business Truth, Workflow Owns Coordination".
/// </summary>
public sealed class WorkflowCompletedPayrollConsumer :
    IConsumer<WorkflowCompletedIntegrationEvent>
{
    private readonly PayrollDbContext _db;
    private readonly ILogger<WorkflowCompletedPayrollConsumer> _logger;

    public WorkflowCompletedPayrollConsumer(PayrollDbContext db, ILogger<WorkflowCompletedPayrollConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<WorkflowCompletedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var msg = context.Message;

        if (msg.EntityType == "ReimbursementClaim")
        {
            await HandleReimbursementCompletion(msg, context.CancellationToken);
        }
        else if (msg.EntityType == "SalaryRevision")
        {
            await HandleSalaryRevisionCompletion(msg, context.CancellationToken);
        }
    }

    private async Task HandleReimbursementCompletion(WorkflowCompletedIntegrationEvent msg, CancellationToken ct)
    {
        var claim = await _db.Set<ReimbursementClaim>().FindAsync([msg.EntityId], ct);
        if (claim == null)
        {
            _logger.LogWarning("Reimbursement Claim {ClaimId} not found for completion.", msg.EntityId);
            return;
        }

        _logger.LogInformation("Finalizing Reimbursement Claim {ClaimId} with status {Status}.", msg.EntityId, msg.FinalStatus);

        if (msg.FinalStatus == WorkflowStatus.Approved.ToString())
        {
            // Note: In a real system, the workflow might pass the final approved amount if changed by finance.
            // For now, we use the original claimed amount.
            claim.FinalizeApproved("system");
        }
        else if (msg.FinalStatus == WorkflowStatus.Rejected.ToString())
        {
            claim.FinalizeRejected("system", "Rejected by workflow coordination.");
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task HandleSalaryRevisionCompletion(WorkflowCompletedIntegrationEvent msg, CancellationToken ct)
    {
        var revision = await _db.Set<Karamchari.Payroll.Domain.SalaryRevisions.SalaryRevision>().FindAsync([msg.EntityId], ct);
        if (revision == null)
        {
            _logger.LogWarning("Salary Revision {RevisionId} not found for completion.", msg.EntityId);
            return;
        }

        _logger.LogInformation("Finalizing Salary Revision {RevisionId} with status {Status}.", msg.EntityId, msg.FinalStatus);

        if (msg.FinalStatus == WorkflowStatus.Approved.ToString())
        {
            revision.Finalize("system");
        }
        else if (msg.FinalStatus == WorkflowStatus.Rejected.ToString())
        {
            revision.Cancel("Rejected by workflow coordination.");
        }

        await _db.SaveChangesAsync(ct);
    }
}
