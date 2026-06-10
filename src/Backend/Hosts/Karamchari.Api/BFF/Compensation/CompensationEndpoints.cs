// -----------------------------------------------------------------------
// <copyright file="CompensationEndpoints.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Compensation.Domain;
using Karamchari.Compensation.Persistence;
using Karamchari.Core.Multitenancy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Compensation;

public static class CompensationEndpoints
{
    public static WebApplication MapCompensationEndpoints(this WebApplication app)
    {
        var bandsGroup = app.MapGroup("/api/v1/compensation/bands").RequireAuthorization();
        bandsGroup.MapGet("/", ListBands).WithName("Compensation.Bands.List");
        bandsGroup.MapPost("/", CreateBand).WithName("Compensation.Bands.Create");
        bandsGroup.MapGet("/{id:guid}", GetBand).WithName("Compensation.Bands.Get");
        bandsGroup.MapPost("/{id:guid}/deactivate", DeactivateBand).WithName("Compensation.Bands.Deactivate");

        var recordsGroup = app.MapGroup("/api/v1/compensation/records").RequireAuthorization();
        recordsGroup.MapGet("/", ListCompensationRecords).WithName("Compensation.Records.List");
        recordsGroup.MapGet("/{employeeId:guid}", GetEmployeeRecord).WithName("Compensation.Records.ByEmployee");

        var equityGroup = app.MapGroup("/api/v1/compensation/equity").RequireAuthorization();
        equityGroup.MapGet("/", GetPayEquityReport).WithName("Compensation.Equity.Report");

        var bonusGroup = app.MapGroup("/api/v1/compensation/bonus-plans").RequireAuthorization();
        bonusGroup.MapGet("/", ListBonusPlans).WithName("Compensation.BonusPlans.List");
        bonusGroup.MapPost("/", CreateBonusPlan).WithName("Compensation.BonusPlans.Create");
        bonusGroup.MapGet("/{planId:guid}", GetBonusPlan).WithName("Compensation.BonusPlans.Get");
        bonusGroup.MapPost("/{planId:guid}/allocations", AllocateBonus).WithName("Compensation.BonusPlans.Allocate");
        bonusGroup.MapPost("/{planId:guid}/approve", ApproveBonusPlan).WithName("Compensation.BonusPlans.Approve");
        bonusGroup.MapPost("/{planId:guid}/pay/{employeeId:guid}", MarkBonusPaid).WithName("Compensation.BonusPlans.Pay");

        return app;
    }

    // ── Bands ───────────────────────────────────────────────────────────────

    private static async Task<IResult> ListBands(
        [FromQuery] string? jobFamily,
        [FromQuery] bool activeOnly = true,
        ClaimsPrincipal user = default!,
        CompensationDbContext db = default!,
        CancellationToken ct = default)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var query = db.Bands.Where(b => b.TenantId == tenantId);
        if (activeOnly) query = query.Where(b => b.IsActive);
        if (!string.IsNullOrWhiteSpace(jobFamily)) query = query.Where(b => b.JobFamily == jobFamily);

        var bands = await query
            .OrderBy(b => b.JobFamily).ThenBy(b => b.CareerLevelCode)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.JobFamily,
                b.CareerLevelCode,
                b.BandType,
                b.CurrencyCode,
                b.MinimumAnnual,
                b.MidpointAnnual,
                b.MaximumAnnual,
                b.EffectiveFrom,
                b.EffectiveTo,
                b.IsActive,
            })
            .ToListAsync(ct);

        return Results.Ok(bands);
    }

    private static async Task<IResult> CreateBand(
        [FromBody] CreateBandRequest req,
        ClaimsPrincipal user,
        CompensationDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        try
        {
            var band = CompensationBand.Create(tenantId, req.Name, req.JobFamily, req.CareerLevelCode,
                req.BandType, req.CurrencyCode, req.MinimumAnnual, req.MidpointAnnual,
                req.MaximumAnnual, req.EffectiveFrom, req.EffectiveTo);
            db.Bands.Add(band);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/compensation/bands/{band.Id}", new { band.Id });
        }
        catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
    }

    private static async Task<IResult> GetBand(Guid id, ClaimsPrincipal user, CompensationDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();
        var band = await db.Bands.FirstOrDefaultAsync(b => b.TenantId == tenantId && b.Id == id, ct);
        return band is null ? Results.NotFound() : Results.Ok(band);
    }

    private static async Task<IResult> DeactivateBand(Guid id, ClaimsPrincipal user, CompensationDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();
        var band = await db.Bands.FirstOrDefaultAsync(b => b.TenantId == tenantId && b.Id == id, ct);
        if (band is null) return Results.NotFound();
        band.Deactivate();
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { band.IsActive });
    }

    // ── Records ─────────────────────────────────────────────────────────────

    private static async Task<IResult> ListCompensationRecords(
        ClaimsPrincipal user, CompensationDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var records = await db.CompensationRecords
            .Where(r => r.TenantId == tenantId)
            .Select(r => new { r.Id, r.EmployeeId, r.CurrentAnnualCTC, r.CurrencyCode, r.LastRevisedOnUtc })
            .ToListAsync(ct);
        return Results.Ok(records);
    }

    private static async Task<IResult> GetEmployeeRecord(
        Guid employeeId, ClaimsPrincipal user, CompensationDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var record = await db.CompensationRecords
            .Include(r => r.Revisions)
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.EmployeeId == employeeId, ct);
        return record is null ? Results.NotFound() : Results.Ok(record);
    }

    // ── Pay Equity ──────────────────────────────────────────────────────────

    /// <summary>
    /// Pay equity report: compares each employee's actual CTC to the band midpoint (compa-ratio).
    /// Groups by job family + career level. Flags below-band (&lt;80%) and above-band (&gt;120%) outliers.
    /// Intentionally gender-neutral at the data layer; consumers may join with employee gender data.
    /// </summary>
    private static async Task<IResult> GetPayEquityReport(
        [FromQuery] string? jobFamily,
        ClaimsPrincipal user,
        CompensationDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var records = await db.CompensationRecords
            .Where(r => r.TenantId == tenantId)
            .Select(r => new { r.EmployeeId, r.CurrentAnnualCTC, r.CurrencyCode })
            .ToListAsync(ct);

        var bands = await db.Bands
            .Where(b => b.TenantId == tenantId && b.IsActive
                && (jobFamily == null || b.JobFamily == jobFamily))
            .Select(b => new
            {
                b.JobFamily,
                b.CareerLevelCode,
                b.CurrencyCode,
                b.MinimumAnnual,
                b.MidpointAnnual,
                b.MaximumAnnual
            })
            .ToListAsync(ct);

        // Naive join: match compensation records by currency to active bands.
        // In production this would join via employee's job family + level.
        var summary = bands.Select(band =>
        {
            var matching = records
                .Where(r => r.CurrencyCode == band.CurrencyCode)
                .ToList();

            if (matching.Count == 0)
                return new
                {
                    band.JobFamily,
                    band.CareerLevelCode,
                    band.MinimumAnnual,
                    band.MidpointAnnual,
                    band.MaximumAnnual,
                    EmployeeCount = 0,
                    AvgCompaRatio = (decimal?)null,
                    BelowBandCount = 0,
                    AboveBandCount = 0,
                };

            var ratios = matching
                .Select(r => band.MidpointAnnual > 0
                    ? Math.Round(r.CurrentAnnualCTC / band.MidpointAnnual * 100, 1) : 0m)
                .ToList();

            return new
            {
                band.JobFamily,
                band.CareerLevelCode,
                band.MinimumAnnual,
                band.MidpointAnnual,
                band.MaximumAnnual,
                EmployeeCount = matching.Count,
                AvgCompaRatio = (decimal?)Math.Round(ratios.Average(), 1),
                BelowBandCount = ratios.Count(r => r < 80m),
                AboveBandCount = ratios.Count(r => r > 120m),
            };
        }).ToList();

        return Results.Ok(new { GeneratedAt = DateTimeOffset.UtcNow, Bands = summary });
    }

    // ── Bonus Plans ─────────────────────────────────────────────────────────

    private static async Task<IResult> ListBonusPlans(
        [FromQuery] int? cycleYear,
        ClaimsPrincipal user,
        CompensationDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var query = db.BonusPlans.Where(p => p.TenantId == tenantId);
        if (cycleYear.HasValue) query = query.Where(p => p.CycleYear == cycleYear.Value);

        var plans = await query
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Type,
                p.CycleYear,
                p.TotalBudget,
                p.CurrencyCode,
                p.IsActive,
                p.CreatedAt,
            })
            .ToListAsync(ct);
        return Results.Ok(plans);
    }

    private static async Task<IResult> CreateBonusPlan(
        [FromBody] CreateBonusPlanRequest req,
        ClaimsPrincipal user,
        CompensationDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        try
        {
            var plan = BonusPlan.Create(tenantId, req.Name, req.Type, req.CycleYear, req.TotalBudget, req.CurrencyCode);
            db.BonusPlans.Add(plan);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/compensation/bonus-plans/{plan.Id}", new { plan.Id });
        }
        catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
    }

    private static async Task<IResult> GetBonusPlan(
        Guid planId, ClaimsPrincipal user, CompensationDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var plan = await db.BonusPlans.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == planId, ct);
        if (plan is null) return Results.NotFound();

        return Results.Ok(new
        {
            plan.Id,
            plan.Name,
            plan.Type,
            plan.CycleYear,
            plan.TotalBudget,
            plan.AllocatedBudget,
            plan.RemainingBudget,
            plan.CurrencyCode,
            plan.IsActive,
            Allocations = plan.Allocations.Select(a => new
            {
                a.Id,
                a.EmployeeId,
                a.TargetAmount,
                a.ActualPaidAmount,
                a.Status,
                a.Notes,
                a.PaidAt
            }),
        });
    }

    private static async Task<IResult> AllocateBonus(
        Guid planId,
        [FromBody] AllocateBonusRequest req,
        ClaimsPrincipal user,
        CompensationDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var plan = await db.BonusPlans.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == planId, ct);
        if (plan is null) return Results.NotFound();

        try
        {
            var alloc = plan.Allocate(req.EmployeeId, req.TargetAmount, req.Notes);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { alloc.Id, alloc.Status, alloc.TargetAmount, plan.RemainingBudget });
        }
        catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
        catch (ArgumentOutOfRangeException ex) { return Results.BadRequest(new { error = ex.Message }); }
    }

    private static async Task<IResult> ApproveBonusPlan(
        Guid planId, ClaimsPrincipal user, CompensationDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var plan = await db.BonusPlans.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == planId, ct);
        if (plan is null) return Results.NotFound();

        try
        {
            plan.ApprovePlan();
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { ApprovedAllocations = plan.Allocations.Count(a => a.Status == BonusAllocationStatus.Approved) });
        }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
    }

    private static async Task<IResult> MarkBonusPaid(
        Guid planId, Guid employeeId,
        [FromBody] MarkBonusPaidRequest req,
        ClaimsPrincipal user,
        CompensationDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var plan = await db.BonusPlans.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == planId, ct);
        if (plan is null) return Results.NotFound();

        try
        {
            plan.MarkPaid(employeeId, req.ActualAmount);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { Status = BonusAllocationStatus.Paid });
        }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
    }

    // ── Request records ──────────────────────────────────────────────────────

    private sealed record CreateBandRequest(
        string Name, string JobFamily, string CareerLevelCode, CompensationBandType BandType,
        string CurrencyCode, decimal MinimumAnnual, decimal MidpointAnnual, decimal MaximumAnnual,
        DateOnly EffectiveFrom, DateOnly? EffectiveTo = null);

    private sealed record CreateBonusPlanRequest(
        string Name, BonusPlanType Type, int CycleYear, decimal TotalBudget, string CurrencyCode);

    private sealed record AllocateBonusRequest(Guid EmployeeId, decimal TargetAmount, string? Notes = null);

    private sealed record MarkBonusPaidRequest(decimal ActualAmount);
}
