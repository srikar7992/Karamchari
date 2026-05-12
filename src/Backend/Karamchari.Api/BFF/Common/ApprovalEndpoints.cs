using System.Security.Claims;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Security;
using Karamchari.Workflow.Domain;
using Karamchari.Workflow.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Common;

/// <summary>
/// Provides a unified approval experience across all platform modules.
/// Implements Step 9: "My Approvals".
/// </summary>
public static class ApprovalEndpoints
{
    public static WebApplication MapApprovalEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/approvals").RequireAuthorization();

        group.MapGet("/my", GetMyApprovals).WithName("Approvals.My");
        group.MapPost("/{stepInstanceId:guid}/approve", ApproveStep).WithName("Approvals.Approve");
        group.MapPost("/{stepInstanceId:guid}/reject", RejectStep).WithName("Approvals.Reject");

        return app;
    }

    private static async Task<IResult> GetMyApprovals(
        ClaimsPrincipal user,
        WorkflowDbContext db,
        CancellationToken ct)
    {
        var roles = user.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
        var tenantId = user.GetTenantId();

        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        // Query pending step instances matching user's roles
        var pendingApprovals = await db.Set<WorkflowStepInstance>()
            .Join(db.WorkflowInstances,
                s => s.WorkflowInstanceId,
                i => i.Id,
                (s, i) => new { s, i })
            .Where(x => x.s.Status == Karamchari.Core.Domain.Workflows.WorkflowStepStatus.Pending
                        && roles.Contains(x.s.ApproverRole)
                        && x.i.TenantId == tenantId)
            .Select(x => new ApprovalInboxItem(
                x.s.Id,
                x.i.Id,
                x.i.EntityType,
                x.i.EntityId,
                x.s.ApproverRole,
                x.s.StepOrder,
                x.i.StartedAt,
                x.i.Status.ToString()))
            .ToListAsync(ct);

        return Results.Ok(pendingApprovals);
    }

    private static async Task<IResult> ApproveStep(
        Guid stepInstanceId,
        [FromBody] ApprovalDecisionRequest request,
        ClaimsPrincipal user,
        WorkflowDbContext db,
        CancellationToken ct)
    {
        var (tenantId, actorId) = user.GetTenantAndEmployee();
        if (actorId is null) return Results.Unauthorized();

        var stepInstance = await db.Set<WorkflowStepInstance>().FindAsync([stepInstanceId], ct);
        if (stepInstance == null) return Results.NotFound();

        var instance = await db.WorkflowInstances
            .Include(i => i.StepInstances)
            .FirstOrDefaultAsync(i => i.Id == stepInstance.WorkflowInstanceId, ct);

        if (instance == null) return Results.NotFound();

        try
        {
            instance.Approve(stepInstanceId, actorId.Value);
            await db.SaveChangesAsync(ct);
            return Results.Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> RejectStep(
        Guid stepInstanceId,
        [FromBody] RejectionDecisionRequest request,
        ClaimsPrincipal user,
        WorkflowDbContext db,
        CancellationToken ct)
    {
        var (tenantId, actorId) = user.GetTenantAndEmployee();
        if (actorId is null) return Results.Unauthorized();

        var stepInstance = await db.Set<WorkflowStepInstance>().FindAsync([stepInstanceId], ct);
        if (stepInstance == null) return Results.NotFound();

        var instance = await db.WorkflowInstances
            .Include(i => i.StepInstances)
            .FirstOrDefaultAsync(i => i.Id == stepInstance.WorkflowInstanceId, ct);

        if (instance == null) return Results.NotFound();

        try
        {
            instance.Reject(stepInstanceId, actorId.Value, request.Reason);
            await db.SaveChangesAsync(ct);
            return Results.Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}

public record ApprovalInboxItem(
    Guid StepInstanceId,
    Guid WorkflowInstanceId,
    string EntityType,
    Guid EntityId,
    string Role,
    int StepOrder,
    DateTimeOffset StartedAt,
    string WorkflowStatus);

public record ApprovalDecisionRequest(string? Note);
public record RejectionDecisionRequest(string Reason);
