// -----------------------------------------------------------------------
// <copyright file="ComplianceGateEndpoints.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Compliance.Contracts;
using Karamchari.Core.Multitenancy;
using Microsoft.AspNetCore.Mvc;

namespace Karamchari.Api.BFF.Compliance;

/// <summary>
/// Compliance enforcement gate endpoints.
/// Callers (scheduling, assignment, shift-start) query these before proceeding with an operation.
/// A 200 with IsBlocked=true means the operation MUST be refused; 200 IsBlocked=false means proceed.
/// </summary>
public static class ComplianceGateEndpoints
{
    public static WebApplication MapComplianceGateEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/compliance/gate").RequireAuthorization();

        group.MapGet("/{employeeId:guid}", CheckGate).WithName("Compliance.Gate.Check");
        group.MapGet("/batch", CheckBatch).WithName("Compliance.Gate.Batch");

        return app;
    }

    /// <summary>
    /// Check whether a specific operation is blocked for an employee.
    /// Query param: operation = Schedule | Assign | StartShift | ProcessPayroll
    /// </summary>
    private static async Task<IResult> CheckGate(
        Guid employeeId,
        [FromQuery] ComplianceOperation operation,
        ClaimsPrincipal user,
        IComplianceGateService gate,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var result = await gate.CheckAsync(tenantId, employeeId, operation, ct);

        return Results.Ok(new
        {
            EmployeeId = employeeId,
            Operation = operation.ToString(),
            result.IsBlocked,
            result.Blocks,
            CheckedAt = DateTimeOffset.UtcNow,
        });
    }

    /// <summary>
    /// Batch gate check: given a list of employee IDs and one operation, returns blocked/allowed per employee.
    /// Useful for scheduling screens that need to mark employees red before the user tries to assign them.
    /// </summary>
    private static async Task<IResult> CheckBatch(
        [FromQuery] string employeeIds,
        [FromQuery] ComplianceOperation operation,
        ClaimsPrincipal user,
        IComplianceGateService gate,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var ids = employeeIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Guid.TryParse(s, out var g) ? (Guid?)g : null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToList();

        if (ids.Count == 0) return Results.BadRequest(new { error = "No valid employee IDs provided." });
        if (ids.Count > 100) return Results.BadRequest(new { error = "Maximum 100 employee IDs per batch." });

        var tasks = ids.Select(async id =>
        {
            var r = await gate.CheckAsync(tenantId, id, operation, ct);
            return new { EmployeeId = id, r.IsBlocked, BlockCount = r.Blocks.Count };
        });

        var results = await Task.WhenAll(tasks);
        return Results.Ok(new
        {
            Operation = operation.ToString(),
            CheckedAt = DateTimeOffset.UtcNow,
            Results = results,
        });
    }
}
