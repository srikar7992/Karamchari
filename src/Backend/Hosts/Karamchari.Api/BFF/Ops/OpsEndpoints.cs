using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Compliance.Domain.Violations;
using Karamchari.Compliance.Persistence;
using Karamchari.Core.Domain.Workflows;
using Karamchari.Core.Multitenancy;
using Karamchari.Integration.Domain;
using Karamchari.Integration.Persistence;
using Karamchari.Workflow.Domain;
using Karamchari.Workflow.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Ops;

/// <summary>
/// Operational observability endpoints.
/// Give operations teams visibility into webhook failure rates,
/// compliance gate block patterns, workflow throughput, and policy governance state
/// without requiring access to broker consoles or raw DB queries.
/// </summary>
public static class OpsEndpoints
{
    public static WebApplication MapOpsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/ops").RequireAuthorization();

        group.MapGet("/health", GetHealth).WithName("Ops.Health");
        group.MapGet("/metrics/workflow", GetWorkflowMetrics).WithName("Ops.Metrics.Workflow");
        group.MapGet("/metrics/webhooks", GetWebhookMetrics).WithName("Ops.Metrics.Webhooks");
        group.MapGet("/metrics/compliance", GetComplianceMetrics).WithName("Ops.Metrics.Compliance");
        group.MapGet("/metrics/policy-governance", GetPolicyGovernanceMetrics).WithName("Ops.Metrics.PolicyGovernance");

        return app;
    }

    /// <summary>
    /// Aggregate health: active definitions, pending instances, open violations, active subscriptions.
    /// One-glance platform status panel for operations.
    /// </summary>
    private static async Task<IResult> GetHealth(
        ClaimsPrincipal user,
        WorkflowDbContext workflowDb,
        ComplianceDbContext complianceDb,
        IntegrationDbContext integrationDb,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var workflowTask = workflowDb.WorkflowDefinitions
            .Where(d => d.TenantId == tenantId && d.IsActive)
            .CountAsync(ct);

        var instanceTask = workflowDb.WorkflowInstances
            .Where(i => i.TenantId == tenantId && i.Status == WorkflowStatus.Pending)
            .CountAsync(ct);

        var violationTask = complianceDb.PolicyViolations
            .Where(v => v.TenantId == tenantId
                && (v.Status == ViolationStatus.Open || v.Status == ViolationStatus.Investigating))
            .CountAsync(ct);

        var subscriptionTask = integrationDb.WebhookSubscriptions
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .CountAsync(ct);

        await Task.WhenAll(workflowTask, instanceTask, violationTask, subscriptionTask);

        return Results.Ok(new
        {
            TenantId = tenantId,
            CheckedAt = DateTimeOffset.UtcNow,
            ActiveWorkflowDefinitions = workflowTask.Result,
            PendingWorkflowInstances = instanceTask.Result,
            OpenComplianceViolations = violationTask.Result,
            ActiveWebhookSubscriptions = subscriptionTask.Result,
        });
    }

    /// <summary>
    /// Workflow instance throughput by status over the last N days.
    /// Surfaces stalled instances (Pending with no activity for 7+ days).
    /// </summary>
    private static async Task<IResult> GetWorkflowMetrics(
        [FromQuery] int days = 30,
        ClaimsPrincipal user = default!,
        WorkflowDbContext db = default!,
        CancellationToken ct = default)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        if (days is < 1 or > 365) return Results.BadRequest(new { error = "days must be between 1 and 365." });

        var since = DateTimeOffset.UtcNow.AddDays(-days);
        var stallCutoff = DateTimeOffset.UtcNow.AddDays(-7);

        var instanceStats = await db.WorkflowInstances
            .Where(i => i.TenantId == tenantId && i.StartedAt >= since)
            .GroupBy(i => i.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(ct);

        var definitionStats = await db.WorkflowDefinitions
            .Where(d => d.TenantId == tenantId)
            .GroupBy(d => d.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(ct);

        // Potentially stalled: Pending instances started > 7 days ago
        var stalledCandidates = await db.WorkflowInstances
            .Where(i => i.TenantId == tenantId
                && i.Status == WorkflowStatus.Pending
                && i.StartedAt < stallCutoff)
            .OrderBy(i => i.StartedAt)
            .Take(10)
            .Select(i => new
            {
                i.Id,
                i.EntityType,
                i.EntityId,
                i.StartedAt,
                AgeDays = (int)(DateTimeOffset.UtcNow - i.StartedAt).TotalDays,
            })
            .ToListAsync(ct);

        return Results.Ok(new
        {
            PeriodDays = days,
            Since = since,
            InstancesByStatus = instanceStats,
            DefinitionsByPolicyStatus = definitionStats,
            PotentiallyStalled = stalledCandidates,
        });
    }

    /// <summary>
    /// Webhook delivery stats: success rate, failure rate, top-failing subscriptions.
    /// </summary>
    private static async Task<IResult> GetWebhookMetrics(
        [FromQuery] int days = 7,
        ClaimsPrincipal user = default!,
        IntegrationDbContext db = default!,
        CancellationToken ct = default)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        if (days is < 1 or > 90) return Results.BadRequest(new { error = "days must be between 1 and 90." });

        var since = DateTimeOffset.UtcNow.AddDays(-days);

        var subscriptions = await db.WebhookSubscriptions
            .Include(s => s.Deliveries)
            .Where(s => s.TenantId == tenantId)
            .ToListAsync(ct);

        var recentDeliveries = subscriptions
            .SelectMany(s => s.Deliveries)
            .Where(d => d.CreatedAt >= since)
            .ToList();

        var deliveryStats = recentDeliveries
            .GroupBy(d => d.Status.ToString())
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToList();

        int total = recentDeliveries.Count;
        int failed = recentDeliveries.Count(d => d.Status == WebhookDeliveryStatus.Failed);
        double failRate = total > 0 ? Math.Round((double)failed / total * 100, 1) : 0;

        var topFailing = subscriptions
            .Select(s => new
            {
                SubscriptionId = s.Id.Value,
                s.TargetUrl,
                FailCount = s.Deliveries.Count(d => d.Status == WebhookDeliveryStatus.Failed && d.CreatedAt >= since),
                TotalCount = s.Deliveries.Count(d => d.CreatedAt >= since),
            })
            .Where(x => x.FailCount > 0)
            .OrderByDescending(x => x.FailCount)
            .Take(10)
            .ToList();

        return Results.Ok(new
        {
            PeriodDays = days,
            Since = since,
            TotalDeliveries = total,
            FailedDeliveries = failed,
            FailureRatePct = failRate,
            DeliveriesByStatus = deliveryStats,
            TopFailingSubscriptions = topFailing,
        });
    }

    /// <summary>
    /// Compliance gate block patterns: severity distribution, top policy codes, affected employees.
    /// Answers "what is compliance enforcement actually blocking?".
    /// </summary>
    private static async Task<IResult> GetComplianceMetrics(
        [FromQuery] int days = 30,
        ClaimsPrincipal user = default!,
        ComplianceDbContext db = default!,
        CancellationToken ct = default)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        if (days is < 1 or > 365) return Results.BadRequest(new { error = "days must be between 1 and 365." });

        var since = DateTime.UtcNow.AddDays(-days);

        var violations = await db.PolicyViolations
            .Where(v => v.TenantId == tenantId && v.DetectedAt >= since)
            .Select(v => new { v.PolicyCode, v.Severity, v.Status, v.EmployeeId })
            .ToListAsync(ct);

        var bySeverity = violations
            .GroupBy(v => v.Severity.ToString())
            .Select(g => new { Severity = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        var byStatus = violations
            .GroupBy(v => v.Status.ToString())
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        var topPolicyCodes = violations
            .GroupBy(v => v.PolicyCode)
            .Select(g => new
            {
                PolicyCode = g.Key,
                Count = g.Count(),
                CriticalCount = g.Count(v => v.Severity == ViolationSeverity.Critical),
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

        int activeBlocking = violations.Count(v =>
            (v.Status == ViolationStatus.Open || v.Status == ViolationStatus.Investigating)
            && v.Severity >= ViolationSeverity.Medium);

        int uniqueAffectedEmployees = violations
            .Where(v => v.Status == ViolationStatus.Open || v.Status == ViolationStatus.Investigating)
            .Select(v => v.EmployeeId)
            .Distinct()
            .Count();

        return Results.Ok(new
        {
            PeriodDays = days,
            Since = since,
            TotalViolations = violations.Count,
            ActiveBlockingViolations = activeBlocking,
            UniqueAffectedEmployees = uniqueAffectedEmployees,
            ViolationsBySeverity = bySeverity,
            ViolationsByStatus = byStatus,
            TopPolicyCodes = topPolicyCodes,
        });
    }

    /// <summary>
    /// Policy governance pipeline: definitions by lifecycle state, expiring soon, not-yet-effective.
    /// Shows Draft/InReview backlog that needs attention before definitions can route.
    /// </summary>
    private static async Task<IResult> GetPolicyGovernanceMetrics(
        ClaimsPrincipal user,
        WorkflowDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var definitions = await db.WorkflowDefinitions
            .Where(d => d.TenantId == tenantId)
            .Select(d => new
            {
                d.Id,
                d.Name,
                d.EntityType,
                Status = d.Status.ToString(),
                d.IsActive,
                d.EffectiveFrom,
                d.EffectiveTo,
            })
            .ToListAsync(ct);

        var byStatus = definitions
            .GroupBy(d => d.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .OrderBy(x => x.Status)
            .ToList();

        var now = DateTime.UtcNow;

        var expiringSoon = definitions
            .Where(d => d.IsActive
                && d.EffectiveTo.HasValue
                && d.EffectiveTo.Value >= now
                && d.EffectiveTo.Value <= now.AddDays(14))
            .Select(d => new
            {
                d.Id,
                d.Name,
                d.EntityType,
                d.EffectiveTo,
                DaysUntilExpiry = (int)(d.EffectiveTo!.Value - now).TotalDays,
            })
            .OrderBy(x => x.DaysUntilExpiry)
            .ToList();

        var notYetEffective = definitions
            .Where(d => d.IsActive && d.EffectiveFrom.HasValue && d.EffectiveFrom.Value > now)
            .Select(d => new { d.Id, d.Name, d.EntityType, d.EffectiveFrom })
            .OrderBy(x => x.EffectiveFrom)
            .ToList();

        return Results.Ok(new
        {
            TotalDefinitions = definitions.Count,
            ByPolicyStatus = byStatus,
            ExpiringSoonWithin14Days = expiringSoon,
            NotYetEffective = notYetEffective,
        });
    }
}
