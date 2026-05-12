using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Core.Domain.Workflows;
using Karamchari.Workflow.Domain;
using Karamchari.Workflow.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Workflow.Consumers;

/// <summary>
/// Orchestrates workflow initiation for platform business events.
/// This is the coordination entry point for business domains.
/// </summary>
public sealed class WorkflowOrchestratorConsumer :
    IConsumer<LeaveRequestCreatedIntegrationEvent>,
    IConsumer<ReimbursementSubmittedIntegrationEvent>
{
    private readonly WorkflowDbContext _db;
    private readonly ILogger<WorkflowOrchestratorConsumer> _logger;

    public WorkflowOrchestratorConsumer(WorkflowDbContext db, ILogger<WorkflowOrchestratorConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<LeaveRequestCreatedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var msg = context.Message;
        _logger.LogInformation("Processing Leave Request {RequestId} for orchestration.", msg.RequestId);

        // 1. Selection: Find or create default definition for the tenant
        var definition = await _db.WorkflowDefinitions
            .Include(d => d.Steps)
            .FirstOrDefaultAsync(d => d.TenantId == msg.TenantId && d.EntityType == "LeaveRequest" && d.IsActive, context.CancellationToken);

        if (definition == null)
        {
            _logger.LogInformation("Creating default LeaveRequest workflow for tenant {TenantId}.", msg.TenantId);
            definition = WorkflowDefinition.Create(msg.TenantId, "LeaveRequest", "Standard Leave Approval");
            definition.AddStep(WorkflowStep.Create(definition.Id, 0, "Manager Approval", false, QuorumRule.All, 1, ["Manager"]));

            _db.WorkflowDefinitions.Add(definition);
            await _db.SaveChangesAsync(context.CancellationToken);
        }

        // 2. Execution: Start Instance
        var instance = WorkflowInstance.Start(msg.TenantId, "LeaveRequest", msg.RequestId, definition);

        _db.WorkflowInstances.Add(instance);
        await _db.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("Workflow Instance {WorkflowId} started for Leave {RequestId}.", instance.Id, msg.RequestId);
    }

    public async Task Consume(ConsumeContext<ReimbursementSubmittedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var msg = context.Message;
        _logger.LogInformation("Processing Reimbursement Claim {ClaimId} for orchestration.", msg.ClaimId);

        var definition = await _db.WorkflowDefinitions
            .Include(d => d.Steps)
            .FirstOrDefaultAsync(d => d.TenantId == msg.TenantId && d.EntityType == "ReimbursementClaim" && d.IsActive, context.CancellationToken);

        if (definition == null)
        {
            _logger.LogInformation("Creating default ReimbursementClaim workflow for tenant {TenantId}.", msg.TenantId);
            definition = WorkflowDefinition.Create(msg.TenantId, "ReimbursementClaim", "Standard Reimbursement Approval");
            definition.AddStep(WorkflowStep.Create(definition.Id, 0, "Manager Approval", false, QuorumRule.All, 1, ["Manager"]));
            definition.AddStep(WorkflowStep.Create(definition.Id, 1, "Finance Approval", false, QuorumRule.All, 1, ["Finance"]));

            _db.WorkflowDefinitions.Add(definition);
            await _db.SaveChangesAsync(context.CancellationToken);
        }

        var instance = WorkflowInstance.Start(msg.TenantId, "ReimbursementClaim", msg.ClaimId, definition);

        _db.WorkflowInstances.Add(instance);
        await _db.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("Workflow Instance {WorkflowId} started for Reimbursement {ClaimId}.", instance.Id, msg.ClaimId);
    }
}
