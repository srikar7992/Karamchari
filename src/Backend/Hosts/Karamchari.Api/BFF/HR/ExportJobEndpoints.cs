// -----------------------------------------------------------------------
// <copyright file="ExportJobEndpoints.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using System.Text.Json;
using Karamchari.Api.BFF;
using Karamchari.Performance.Domain.Reporting;
using Karamchari.Performance.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.HR;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public static class ExportJobEndpoints
{
    private const int MaxConcurrentExportsPerTenant = 3;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static WebApplication MapExportJobs(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/hr/reports").RequireAuthorization();

        group.MapPost("/{type}", CreateExportJob).WithName("Reports.Create");
        group.MapGet("/{jobId:guid}/status", GetExportJobStatus).WithName("Reports.Status");
        group.MapGet("/{jobId:guid}/download", DownloadExportJob).WithName("Reports.Download");
        group.MapDelete("/{jobId:guid}", CancelExportJob).WithName("Reports.Cancel");

        return app;
    }

    private static async Task<IResult> CreateExportJob(
        string type,
        [FromBody] JsonElement parameters,
        ClaimsPrincipal user,
        PerformanceDbContext db,
        CancellationToken ct)
    {
        if (!Enum.TryParse<ReportType>(type, ignoreCase: true, out var reportType))
            return Results.BadRequest(new { error = $"Unknown report type: {type}" });

        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null)
            return Results.Unauthorized();

        // Enforce concurrent export quota: max 3 active jobs per tenant.
        var activeCount = await db.ExportJobs
            .CountAsync(j => j.TenantId == tenantId
                          && (j.Status == ExportJobStatus.Queued || j.Status == ExportJobStatus.Processing),
                ct);

        if (activeCount >= MaxConcurrentExportsPerTenant)
            return Results.StatusCode(429);

        var parametersJson = parameters.ValueKind == JsonValueKind.Undefined
            ? "{}"
            : parameters.GetRawText();

        var paramHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(parametersJson)));

        var idempotencyKey = $"{tenantId}:{reportType}:{paramHash}:{employeeId}";
        var dedupCutoff = DateTimeOffset.UtcNow.AddSeconds(-60);

        // Return existing job if a duplicate arrived within the 60-second window.
        var existing = await db.ExportJobs
            .Where(j => j.TenantId == tenantId
                     && j.IdempotencyKey == idempotencyKey
                     && j.CreatedOnUtc >= dedupCutoff)
            .Select(j => new { j.Id, j.Status })
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
            return Results.Accepted($"/api/v1/hr/reports/{existing.Id}/status",
                new { jobId = existing.Id, status = existing.Status.ToString() });

        var job = ExportJob.Create(tenantId, employeeId.Value, reportType, parametersJson, idempotencyKey);
        db.ExportJobs.Add(job);
        await db.SaveChangesAsync(ct);

        return Results.Accepted($"/api/v1/hr/reports/{job.Id}/status",
            new { jobId = job.Id, status = job.Status.ToString() });
    }

    private static async Task<IResult> GetExportJobStatus(
        Guid jobId,
        ClaimsPrincipal user,
        PerformanceDbContext db,
        CancellationToken ct)
    {
        var tenantId = user.GetTenantId();
        if (tenantId is null) return Results.Unauthorized();

        var job = await db.ExportJobs
            .AsNoTracking()
            .Where(j => j.Id == jobId && j.TenantId == tenantId)
            .Select(j => new
            {
                j.Id,
                j.ReportType,
                j.Status,
                j.ErrorMessage,
                j.CreatedOnUtc,
                j.CompletedOnUtc,
                HasDownload = j.BlobUri != null,
            })
            .FirstOrDefaultAsync(ct);

        return job is null ? Results.NotFound() : Results.Ok(job);
    }

    private static async Task<IResult> DownloadExportJob(
        Guid jobId,
        ClaimsPrincipal user,
        PerformanceDbContext db,
        CancellationToken ct)
    {
        var tenantId = user.GetTenantId();
        if (tenantId is null) return Results.Unauthorized();

        var job = await db.ExportJobs
            .AsNoTracking()
            .Where(j => j.Id == jobId && j.TenantId == tenantId)
            .Select(j => new { j.Status, j.BlobUri })
            .FirstOrDefaultAsync(ct);

        if (job is null) return Results.NotFound();
        if (job.Status != ExportJobStatus.Ready) return Results.BadRequest(new { error = "Export not ready." });
        if (string.IsNullOrWhiteSpace(job.BlobUri)) return Results.Problem("Blob URI missing for ready job.");

        // Phase 1: redirect to blob URI directly.
        // Phase 2: generate a short-lived SAS URL and redirect to that.
        return Results.Redirect(job.BlobUri);
    }

    private static async Task<IResult> CancelExportJob(
        Guid jobId,
        ClaimsPrincipal user,
        PerformanceDbContext db,
        CancellationToken ct)
    {
        var tenantId = user.GetTenantId();
        if (tenantId is null) return Results.Unauthorized();

        var job = await db.ExportJobs
            .Where(j => j.Id == jobId && j.TenantId == tenantId)
            .FirstOrDefaultAsync(ct);

        if (job is null) return Results.NotFound();

        try
        {
            job.Cancel();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { status = job.Status.ToString() });
    }
}
