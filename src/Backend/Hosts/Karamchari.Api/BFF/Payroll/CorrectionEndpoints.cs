// -----------------------------------------------------------------------
// <copyright file="CorrectionEndpoints.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Api.Middleware;
using Karamchari.Payroll.Contracts;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.Corrections;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Payroll;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public static class CorrectionEndpoints
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static WebApplication MapCorrectionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/payroll/corrections").RequireAuthorization();

        group.MapPost("/trigger", TriggerCorrection).WithName("Correction.Trigger").WithIdempotency();
        group.MapGet("/{id:guid}", GetCorrection).WithName("Correction.Get");
        group.MapGet("/", ListCorrections).WithName("Correction.List");
        group.MapPost("/{id:guid}/approve", ApproveCorrection).WithName("Correction.Approve").WithIdempotency();
        group.MapPost("/{id:guid}/reject", RejectCorrection).WithName("Correction.Reject").WithIdempotency();

        return app;
    }

    private static async Task<IResult> TriggerCorrection(
        [FromBody] CreateCorrectionRequest request,
        ClaimsPrincipal user,
        PayrollDbContext db,
        CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        if (!Enum.TryParse<CorrectionType>(request.Type, out var type))
            return Results.BadRequest($"Invalid correction type: {request.Type}");

        if (!Enum.TryParse<CorrectionScope>(request.Scope, out var scope))
            return Results.BadRequest($"Invalid correction scope: {request.Scope}");

        var now = DateTime.UtcNow;
        var correction = PayrollCorrection.Create(
            tenantId, request.EmployeeId, request.EmployeeName,
            type, scope, request.AffectedPeriodName,
            now.Year, now.Month,
            request.Reason, request.ChangeDetailsJson,
            afterBankDisbursement: false, afterTaxFiling: false, afterEmployeeExit: false,
            requestedBy: employeeId.ToString()!);

        db.Set<PayrollCorrection>().Add(correction);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/v1/payroll/corrections/{correction.Id}", MapToDto(correction));
    }

    private static async Task<IResult> GetCorrection(
        Guid id, PayrollDbContext db, CancellationToken ct)
    {
        var correction = await db.Set<PayrollCorrection>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        return correction is null ? Results.NotFound() : Results.Ok(MapToDto(correction));
    }

    private static async Task<IResult> ListCorrections(
        PayrollDbContext db,
        [FromQuery] string? status,
        [FromQuery] Guid? employeeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = db.Set<PayrollCorrection>().AsNoTracking();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<CorrectionStatus>(status, out var s))
            query = query.Where(c => c.Status == s);

        if (employeeId.HasValue)
            query = query.Where(c => c.EmployeeId == employeeId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Results.Ok(new { Items = items.Select(MapToDto), Total = total, Page = page });
    }

    private static async Task<IResult> ApproveCorrection(
        Guid id,
        [FromBody] string note,
        ClaimsPrincipal user,
        PayrollDbContext db,
        IPublishEndpoint bus,
        CancellationToken ct)
    {
        var correction = await db.Set<PayrollCorrection>().FirstOrDefaultAsync(c => c.Id == id, ct);
        if (correction is null) return Results.NotFound();

        var approvedBy = user.GetEmployeeIdString();
        if (approvedBy is null) return Results.Unauthorized();

        correction.Approve(approvedBy);

        await db.SaveChangesAsync(ct);

        await bus.Publish(new CorrectionApprovedIntegrationEventV1
        {
            CorrectionId = correction.Id,
            TenantId = correction.TenantId,
            EmployeeId = correction.EmployeeId,
            CorrectionType = correction.Type.ToString(),
            CorrectionScope = correction.Scope.ToString(),
            AffectedPeriodName = correction.AffectedPeriodName,
            OccurredOnUtc = DateTimeOffset.UtcNow
        }, ct);

        return Results.Ok(MapToDto(correction));
    }

    private static async Task<IResult> RejectCorrection(
        Guid id,
        [FromBody] string reason,
        ClaimsPrincipal user,
        PayrollDbContext db,
        IPublishEndpoint bus,
        CancellationToken ct)
    {
        var correction = await db.Set<PayrollCorrection>().FirstOrDefaultAsync(c => c.Id == id, ct);
        if (correction is null) return Results.NotFound();

        var rejectedBy = user.GetEmployeeIdString();
        if (rejectedBy is null) return Results.Unauthorized();

        correction.Reject(rejectedBy, reason);

        await db.SaveChangesAsync(ct);

        return Results.Ok(MapToDto(correction));
    }
    private static CorrectionDto MapToDto(PayrollCorrection c) =>
        new(c.Id, c.EmployeeId, c.EmployeeName, c.Type.ToString(),
            c.Scope.ToString(), c.AffectedPeriodName, c.Status.ToString(),
            c.DifferentialAmount, c.CreatedAtUtc);
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record CorrectionDto(
    Guid Id, Guid EmployeeId, string EmployeeName, string Type,
    string Scope, string PeriodName, string Status, decimal? Differential,
    DateTimeOffset CreatedAt);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record CreateCorrectionRequest(
    Guid EmployeeId, string EmployeeName, string Type, string Scope,
    string AffectedPeriodName, string Reason, string ChangeDetailsJson);
