// -----------------------------------------------------------------------
// <copyright file="AssetManagementEndpoints.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.AssetManagement.Domain;
using Karamchari.AssetManagement.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.AssetManagement;

public static class AssetManagementEndpoints
{
    public static WebApplication MapAssetManagementEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/assets").RequireAuthorization();

        group.MapGet("/categories", GetCategories).WithName("Assets.Categories.List");
        group.MapPost("/categories", CreateCategory).WithName("Assets.Categories.Create");
        group.MapPost("/", RegisterAsset).WithName("Assets.Register");
        group.MapGet("/", ListAssets).WithName("Assets.List");
        group.MapGet("/{assetId:guid}", GetAsset).WithName("Assets.Get");
        group.MapPost("/{assetId:guid}/assign", AssignAsset).WithName("Assets.Assign");
        group.MapPost("/{assetId:guid}/return", ReturnAsset).WithName("Assets.Return");
        group.MapPost("/{assetId:guid}/retire", RetireAsset).WithName("Assets.Retire");
        group.MapGet("/employee/{employeeId:guid}", GetEmployeeAssets).WithName("Assets.ByEmployee");
        group.MapPost("/{assetId:guid}/maintenance", ScheduleMaintenance).WithName("Assets.Maintenance.Schedule");
        group.MapGet("/{assetId:guid}/maintenance", GetMaintenanceHistory).WithName("Assets.Maintenance.History");
        group.MapPost("/{assetId:guid}/maintenance/{recordId:guid}/start", StartMaintenance).WithName("Assets.Maintenance.Start");
        group.MapPost("/{assetId:guid}/maintenance/{recordId:guid}/complete", CompleteMaintenance).WithName("Assets.Maintenance.Complete");
        group.MapPost("/{assetId:guid}/depreciation", CreateDepreciationSchedule).WithName("Assets.Depreciation.Create");
        group.MapGet("/{assetId:guid}/depreciation", GetDepreciation).WithName("Assets.Depreciation.Get");
        group.MapPost("/{assetId:guid}/depreciation/record-period", RecordDepreciationPeriod).WithName("Assets.Depreciation.RecordPeriod");

        return app;
    }

    private static async Task<IResult> GetCategories(ClaimsPrincipal user, AssetManagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var cats = await db.AssetCategories
            .Where(c => c.TenantId == tenantId && c.IsActive)
            .Select(c => new { c.Id, c.Name, c.RequiresReturnOnOffboarding })
            .ToListAsync(ct);
        return Results.Ok(cats);
    }

    private static async Task<IResult> CreateCategory([FromBody] CreateCategoryRequest req, ClaimsPrincipal user, AssetManagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var cat = AssetCategory.Create(tenantId, req.Name, req.RequiresReturn);
        db.AssetCategories.Add(cat);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/assets/categories/{cat.Id.Value}", new { cat.Id });
    }

    private static async Task<IResult> RegisterAsset([FromBody] RegisterAssetRequest req, ClaimsPrincipal user, AssetManagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var asset = Asset.Register(tenantId, new AssetCategoryId(req.CategoryId), req.Name, req.SerialNumber, req.AssetTag, req.PurchaseValue, req.PurchasedOn, req.Condition);
        db.Assets.Add(asset);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/assets/{asset.Id.Value}", new { asset.Id });
    }

    private static async Task<IResult> ListAssets([FromQuery] string? status, ClaimsPrincipal user, AssetManagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var query = db.Assets.Where(a => a.TenantId == tenantId);
        if (Enum.TryParse<AssetStatus>(status, ignoreCase: true, out var s)) query = query.Where(a => a.Status == s);

        var assets = await query
            .Select(a => new { a.Id, a.Name, a.SerialNumber, a.AssetTag, a.Status, a.Condition, a.CategoryId, a.PurchaseValue })
            .ToListAsync(ct);
        return Results.Ok(assets);
    }

    private static async Task<IResult> GetAsset(Guid assetId, ClaimsPrincipal user, AssetManagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var asset = await db.Assets.FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == new AssetId(assetId), ct);
        return asset is null ? Results.NotFound() : Results.Ok(asset);
    }

    private static async Task<IResult> AssignAsset(Guid assetId, [FromBody] AssignAssetRequest req, ClaimsPrincipal user, AssetManagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var asset = await db.Assets.FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == new AssetId(assetId), ct);
        if (asset is null) return Results.NotFound();

        asset.AssignTo(req.EmployeeId, req.AssignedOn, req.Notes);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { asset.Status });
    }

    private static async Task<IResult> ReturnAsset(Guid assetId, [FromBody] ReturnAssetRequest req, ClaimsPrincipal user, AssetManagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var asset = await db.Assets.FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == new AssetId(assetId), ct);
        if (asset is null) return Results.NotFound();

        asset.RecordReturn(req.ReturnedOn, req.Condition, req.Notes);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { asset.Status, asset.Condition });
    }

    private static async Task<IResult> RetireAsset(Guid assetId, [FromBody] RetireAssetRequest req, ClaimsPrincipal user, AssetManagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var asset = await db.Assets.FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == new AssetId(assetId), ct);
        if (asset is null) return Results.NotFound();

        asset.Retire(req.Reason);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { asset.Status });
    }

    private static async Task<IResult> GetEmployeeAssets(Guid employeeId, ClaimsPrincipal user, AssetManagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var assets = await db.Assets
            .Where(a => a.TenantId == tenantId && a.Status == AssetStatus.Assigned)
            .ToListAsync(ct);

        var employeeAssets = assets
            .Where(a => a.CurrentAssignment?.EmployeeId == employeeId)
            .Select(a => new { a.Id, a.Name, a.SerialNumber, a.AssetTag, a.Condition, AssignedOn = a.CurrentAssignment!.AssignedOn });

        return Results.Ok(employeeAssets);
    }

    private static async Task<IResult> ScheduleMaintenance(Guid assetId, [FromBody] ScheduleMaintenanceRequest req, ClaimsPrincipal user, AssetManagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var asset = await db.Assets.FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == new AssetId(assetId), ct);
        if (asset is null) return Results.NotFound();

        var record = new AssetMaintenanceRecord(
            new MaintenanceRecordId(Guid.NewGuid()), new AssetId(assetId),
            req.Type, req.Description, req.ScheduledOn, req.WarrantyExpiresOn,
            req.Vendor, req.EstimatedCost);
        db.MaintenanceRecords.Add(record);

        if (req.SendForMaintenance) asset.SendForMaintenance();
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/assets/{assetId}/maintenance/{record.Id.Value}", new { record.Id, record.Status });
    }

    private static async Task<IResult> GetMaintenanceHistory(Guid assetId, ClaimsPrincipal user, AssetManagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var records = await db.MaintenanceRecords
            .Where(r => r.AssetId == new AssetId(assetId))
            .OrderByDescending(r => r.ScheduledOn)
            .Select(r => new { r.Id, r.Type, r.Status, r.ScheduledOn, r.CompletedOn, r.EstimatedCost, r.ActualCost, r.Vendor, r.IsUnderWarranty })
            .ToListAsync(ct);
        return Results.Ok(records);
    }

    private static async Task<IResult> StartMaintenance(Guid assetId, Guid recordId, ClaimsPrincipal user, AssetManagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var record = await db.MaintenanceRecords.FirstOrDefaultAsync(r => r.Id == new MaintenanceRecordId(recordId) && r.AssetId == new AssetId(assetId), ct);
        if (record is null) return Results.NotFound();

        record.StartWork();
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { record.Status });
    }

    private static async Task<IResult> CompleteMaintenance(Guid assetId, Guid recordId, [FromBody] CompleteMaintenanceRequest req, ClaimsPrincipal user, AssetManagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var asset = await db.Assets.FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == new AssetId(assetId), ct);
        var record = await db.MaintenanceRecords.FirstOrDefaultAsync(r => r.Id == new MaintenanceRecordId(recordId) && r.AssetId == new AssetId(assetId), ct);
        if (asset is null || record is null) return Results.NotFound();

        record.Complete(req.CompletedOn, req.ActualCost, req.Notes);
        asset.ReturnFromMaintenance(req.ConditionAfter);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { MaintenanceStatus = record.Status, AssetStatus = asset.Status, asset.Condition });
    }

    private static async Task<IResult> CreateDepreciationSchedule(Guid assetId, [FromBody] CreateDepreciationRequest req, ClaimsPrincipal user, AssetManagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var exists = await db.DepreciationSchedules.AnyAsync(d => d.TenantId == tenantId && d.AssetId == new AssetId(assetId), ct);
        if (exists) return Results.Conflict(new { error = "Depreciation schedule already exists for this asset." });

        var schedule = AssetDepreciationSchedule.Create(tenantId, new AssetId(assetId),
            req.PurchaseValue, req.ResidualValue, req.UsefulLifeYears, req.Method, req.StartDate);
        db.DepreciationSchedules.Add(schedule);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/assets/{assetId}/depreciation", new { schedule.Id, schedule.CurrentBookValue });
    }

    private static async Task<IResult> GetDepreciation(Guid assetId, ClaimsPrincipal user, AssetManagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var schedule = await db.DepreciationSchedules.FirstOrDefaultAsync(d => d.TenantId == tenantId && d.AssetId == new AssetId(assetId), ct);
        if (schedule is null) return Results.NotFound();

        return Results.Ok(new
        {
            schedule.Id,
            schedule.PurchaseValue,
            schedule.ResidualValue,
            schedule.CurrentBookValue,
            schedule.AccumulatedDepreciation,
            schedule.UsefulLifeYears,
            schedule.Method,
            schedule.StartDate,
            Periods = schedule.Periods,
        });
    }

    private static async Task<IResult> RecordDepreciationPeriod(Guid assetId, [FromBody] RecordPeriodRequest req, ClaimsPrincipal user, AssetManagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var schedule = await db.DepreciationSchedules.FirstOrDefaultAsync(d => d.TenantId == tenantId && d.AssetId == new AssetId(assetId), ct);
        if (schedule is null) return Results.NotFound();

        var entry = schedule.RecordPeriod(req.Year);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { entry.Year, entry.Amount, entry.BookValueAfter });
    }

    private sealed record CreateCategoryRequest(string Name, bool RequiresReturn = true);
    private sealed record RegisterAssetRequest(Guid CategoryId, string Name, string? SerialNumber, string? AssetTag, decimal PurchaseValue, DateOnly PurchasedOn, AssetCondition Condition);
    private sealed record AssignAssetRequest(Guid EmployeeId, DateOnly AssignedOn, string? Notes);
    private sealed record ReturnAssetRequest(DateOnly ReturnedOn, AssetCondition Condition, string? Notes);
    private sealed record RetireAssetRequest(string Reason);
    private sealed record ScheduleMaintenanceRequest(MaintenanceType Type, string Description, DateOnly ScheduledOn, DateOnly? WarrantyExpiresOn, string? Vendor, decimal? EstimatedCost, bool SendForMaintenance = false);
    private sealed record CompleteMaintenanceRequest(DateOnly CompletedOn, decimal ActualCost, string? Notes, AssetCondition ConditionAfter);
    private sealed record CreateDepreciationRequest(decimal PurchaseValue, decimal ResidualValue, int UsefulLifeYears, DepreciationMethod Method, DateOnly StartDate);
    private sealed record RecordPeriodRequest(int Year);
}
