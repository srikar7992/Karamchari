namespace Karamchari.TimeAttendance.Services;

using Karamchari.TimeAttendance.Domain.Approval;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Resolves which approval workflow definition applies for a given entity + context.
/// Builds the LeaveApproval chain from the resolved definition.
/// Never embed workflow construction inside LeaveApproval aggregate.
/// </summary>
public interface IApprovalWorkflowResolver
{
    Task<ApprovalWorkflowDefinition?> ResolveDefinitionAsync(
        string entityType,
        ApprovalWorkflowContext context,
        CancellationToken ct = default);

    Task<LeaveApproval> BuildApprovalChainAsync(
        Guid leaveRequestId,
        ApprovalWorkflowContext context,
        CancellationToken ct = default);
}

public sealed class ApprovalWorkflowContext
{
    public required Guid EmployeeId { get; init; }
    public string? EmployeeGrade { get; init; }
    public double RequestedDays { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? LeaveTypeId { get; init; }

    /// <summary>Approver IDs keyed by role. Populated before building chain.</summary>
    public Dictionary<ApproverRole, Guid> ApproverMap { get; init; } = [];
}

public sealed class ApprovalWorkflowResolver(TimeAttendanceDbContext db) : IApprovalWorkflowResolver
{
    private readonly TimeAttendanceDbContext _db = db;

    public async Task<ApprovalWorkflowDefinition?> ResolveDefinitionAsync(
        string entityType,
        ApprovalWorkflowContext context,
        CancellationToken ct = default)
    {
        // Find the most specific active workflow definition for this entity type
        return await _db.ApprovalWorkflowDefinitions
            .Include(d => d.Steps)
            .Where(d => d.EntityType == entityType && d.IsActive && d.TenantId == _db.CurrentTenantId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<LeaveApproval> BuildApprovalChainAsync(
        Guid leaveRequestId,
        ApprovalWorkflowContext context,
        CancellationToken ct = default)
    {
        ApprovalWorkflowDefinition? definition = await ResolveDefinitionAsync("Leave", context, ct);
        var chain = LeaveApproval.Create(leaveRequestId);

        if (definition is null)
        {
            // Fallback: single-step manager approval
            if (context.ApproverMap.TryGetValue(ApproverRole.Manager, out Guid managerId))
                chain.AddStep(1, managerId, ApproverRole.Manager);
            return chain;
        }

        foreach (ApprovalStepDefinition? step in definition.Steps.OrderBy(s => s.Sequence))
        {
            if (!ShouldIncludeStep(step, context)) continue;

            if (context.ApproverMap.TryGetValue(step.Role, out Guid approverId))
                chain.AddStep(step.Sequence, approverId, step.Role);
        }

        return chain;
    }

    private static bool ShouldIncludeStep(ApprovalStepDefinition step, ApprovalWorkflowContext ctx)
    {
        if (string.IsNullOrWhiteSpace(step.ConditionExpression)) return true;

        // Simple condition evaluator — extend with expression parser if needed
        return step.ConditionExpression switch
        {
            var e when e.Contains("ActualDays >") => TryParseThreshold(e, "ActualDays >", ctx.RequestedDays),
            var e when e.Contains("ActualDays >=") => TryParseThreshold(e, "ActualDays >=", ctx.RequestedDays, orEqual: true),
            _ => true // Unknown condition → include step (safe default)
        };
    }

    private static bool TryParseThreshold(string expression, string prefix, double value, bool orEqual = false)
    {
        string part = expression.Replace(prefix, "").Trim();
        if (!double.TryParse(part, out double threshold)) return true;
        return orEqual ? value >= threshold : value > threshold;
    }
}
