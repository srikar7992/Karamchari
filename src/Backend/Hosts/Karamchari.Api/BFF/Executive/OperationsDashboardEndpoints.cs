// -----------------------------------------------------------------------
// <copyright file="OperationsDashboardEndpoints.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Jobs;
using Karamchari.Core.Projections;
using Microsoft.AspNetCore.Mvc;

namespace Karamchari.Api.BFF.Executive;

/// <summary>
/// Provides operational dashboards for projection health, job status, and data scale.
/// Implements Step 33.
/// </summary>
public static class OperationsDashboardEndpoints
{
    public static WebApplication MapOperationsDashboardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/ops/dashboards").RequireAuthorization();

        group.MapGet("/projections", GetProjectionHealth).WithName("Ops.Dashboards.Projections");
        group.MapGet("/jobs", GetJobStatus).WithName("Ops.Dashboards.Jobs");
        group.MapPost("/projections/{name}/rebuild", RebuildProjection).WithName("Ops.Dashboards.Rebuild");

        return app;
    }

    private static IResult GetProjectionHealth(IProjectionRegistry registry)
    {
        var health = registry.GetAllMetadata().Select(m => new
        {
            m.ProjectionName,
            m.OwningCapability,
            m.Category,
            m.ConsistencyType,
            Status = "Healthy", // Placeholder
            LastRefreshed = DateTimeOffset.UtcNow.AddMinutes(-2), // Placeholder
            RefreshTarget = m.RefreshTarget
        });

        return Results.Ok(health);
    }

    private static IResult GetJobStatus()
    {
        // Mock data for demo
        var jobs = new[]
        {
            new JobStatusProjection(Guid.NewGuid(), "PayrollReconciliation", "Payroll", JobStatus.Running, DateTimeOffset.UtcNow.AddMinutes(-5), null, 0, null),
            new JobStatusProjection(Guid.NewGuid(), "LeaveAccrual", "TimeAttendance", JobStatus.Completed, DateTimeOffset.UtcNow.AddHours(-12), DateTimeOffset.UtcNow.AddHours(-11), 0, null)
        };

        return Results.Ok(jobs);
    }

    private static async Task<IResult> RebuildProjection(
        string name,
        [FromQuery] string tenantId,
        IProjectionRegistry registry,
        CancellationToken ct)
    {
        var rebuilder = registry.GetRebuilder(name);
        if (rebuilder == null) return Results.NotFound($"Projection {name} not found.");

        await rebuilder.RebuildAsync(tenantId, ct: ct);
        return Results.Accepted();
    }
}
