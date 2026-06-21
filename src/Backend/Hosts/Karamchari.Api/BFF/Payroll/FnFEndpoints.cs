// -----------------------------------------------------------------------
// <copyright file="FnFEndpoints.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Api.Middleware;
using Karamchari.Payroll.Contracts;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.FnF;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Payroll;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public static class FnFEndpoints
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static WebApplication MapFnFEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/payroll/fnf").RequireAuthorization();

        group.MapPost("/initiate", InitiateSettlement).WithName("FnF.Initiate").WithIdempotency();
        group.MapGet("/{id:guid}", GetSettlement).WithName("FnF.Get");
        group.MapGet("/", ListSettlements).WithName("FnF.List");
        group.MapPost("/{id:guid}/approve", ApproveSettlement).WithName("FnF.Approve").WithIdempotency();
        group.MapPost("/{id:guid}/reject", RejectSettlement).WithName("FnF.Reject").WithIdempotency();

        return app;
    }

    private static async Task<IResult> InitiateSettlement(
        [FromBody] InitiateFnFRequest request,
        ClaimsPrincipal user,
        PayrollDbContext db,
        IPublishEndpoint bus,
        CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        if (!Enum.TryParse<ExitType>(request.ExitType, out var exitType))
            return Results.BadRequest($"Invalid exit type: {request.ExitType}");

        var settlement = FnFSettlement.Initiate(
            tenantId, request.EmployeeId, request.EmployeeName,
            exitType, request.LastWorkingDay, employeeId.ToString()!);

        db.Set<FnFSettlement>().Add(settlement);
        await db.SaveChangesAsync(ct);

        await bus.Publish(new FnFSettlementInitiatedIntegrationEventV1
        {
            SettlementId = settlement.Id,
            TenantId = tenantId,
            EmployeeId = request.EmployeeId,
            ExitType = exitType.ToString(),
            LastWorkingDay = request.LastWorkingDay,
            OccurredOnUtc = DateTimeOffset.UtcNow
        }, ct);

        return Results.Created($"/api/v1/payroll/fnf/{settlement.Id}", MapToDto(settlement));
    }

    private static async Task<IResult> GetSettlement(
        Guid id, PayrollDbContext db, CancellationToken ct)
    {
        var settlement = await db.Set<FnFSettlement>()
            .AsNoTracking()
            .Include(s => s.LineItems)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        return settlement is null ? Results.NotFound() : Results.Ok(MapToDto(settlement));
    }

    private static async Task<IResult> ListSettlements(
        PayrollDbContext db,
        [FromQuery] string? status,
        [FromQuery] Guid? employeeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = db.Set<FnFSettlement>().AsNoTracking();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<FnFStatus>(status, out var s))
            query = query.Where(x => x.Status == s);

        if (employeeId.HasValue)
            query = query.Where(x => x.EmployeeId == employeeId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Results.Ok(new { Items = items.Select(MapToDto), Total = total, Page = page });
    }

    private static async Task<IResult> ApproveSettlement(
        Guid id,
        ClaimsPrincipal user,
        PayrollDbContext db,
        IPublishEndpoint bus,
        CancellationToken ct)
    {
        var settlement = await db.Set<FnFSettlement>().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (settlement is null) return Results.NotFound();

        var approvedBy = user.GetEmployeeIdString();
        if (approvedBy is null) return Results.Unauthorized();

        settlement.Approve(approvedBy);

        await db.SaveChangesAsync(ct);

        await bus.Publish(new FnFSettlementApprovedIntegrationEventV1
        {
            SettlementId = settlement.Id,
            TenantId = settlement.TenantId,
            EmployeeId = settlement.EmployeeId,
            NetSettlementAmount = settlement.NetSettlementAmount,
            ApprovedBy = approvedBy,
            OccurredOnUtc = DateTimeOffset.UtcNow
        }, ct);

        return Results.Ok(MapToDto(settlement));
    }

    private static async Task<IResult> RejectSettlement(
        Guid id,
        [FromBody] string reason,
        ClaimsPrincipal user,
        PayrollDbContext db,
        CancellationToken ct)
    {
        var settlement = await db.Set<FnFSettlement>().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (settlement is null) return Results.NotFound();

        var rejectedBy = user.GetEmployeeIdString();
        if (rejectedBy is null) return Results.Unauthorized();

        settlement.Cancel(reason);
        await db.SaveChangesAsync(ct);
        return Results.Ok(MapToDto(settlement));
    }

    private static async Task<IResult> DisburseSettlement(
        Guid id,
        IPublishEndpoint bus,
        CancellationToken ct)
    {
        await bus.Publish(new DisburseFnFCommand { SettlementId = id }, ct);
        return Results.Accepted();
    }

    private static FnFDto MapToDto(FnFSettlement s) =>
        new(s.Id, s.EmployeeId, s.EmployeeName, s.ExitType.ToString(),
            s.Status.ToString(), s.NetSettlementAmount, s.LastWorkingDay, s.CreatedAtUtc);
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record FnFDto(
    Guid Id, Guid EmployeeId, string EmployeeName, string ExitType,
    string Status, decimal NetAmount, DateOnly LastWorkingDay, DateTimeOffset CreatedAt);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record InitiateFnFRequest(
    Guid EmployeeId, string EmployeeName, string ExitType, DateOnly LastWorkingDay);
