// -----------------------------------------------------------------------
// <copyright file="WorkflowApprovalEndpoints.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Core.Multitenancy;
using Karamchari.Workflow.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Workflow;

/// <summary>
/// Workflow definition approval history endpoints.
///
/// Answers the regulatory audit question: who changed a policy rule, when, and why?
/// Example use-case: a compliance auditor queries the approval chain for a
/// "Bonus > 20%" compensation rule to verify appropriate authorization.
/// </summary>
public static class WorkflowApprovalEndpoints
{
    public static WebApplication MapWorkflowApprovalEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/workflow/definitions").RequireAuthorization();

        group.MapGet("/{id:guid}/approvals", GetApprovalHistory)
            .WithName("Workflow.Definitions.ApprovalHistory");

        return app;
    }

    /// <summary>
    /// Returns the full approval history for a workflow definition.
    /// Ordered chronologically (oldest first).
    /// </summary>
    private static async Task<IResult> GetApprovalHistory(
        Guid id,
        ClaimsPrincipal user,
        WorkflowDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var def = await db.WorkflowDefinitions
            .Include("ApprovalHistory") // use string to avoid owned-entity navigation issues
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Id == id, ct);

        if (def is null) return Results.NotFound();

        var history = def.ApprovalHistory
            .OrderBy(e => e.OccurredAt)
            .Select(e => new
            {
                e.Id,
                e.ActorId,
                FromStatus = e.FromStatus.ToString(),
                ToStatus = e.ToStatus.ToString(),
                e.OccurredAt,
                e.Notes,
            })
            .ToList();

        return Results.Ok(new
        {
            DefinitionId = def.Id,
            DefinitionName = def.Name,
            CurrentStatus = def.Status.ToString(),
            TransitionCount = history.Count,
            History = history,
        });
    }
}
