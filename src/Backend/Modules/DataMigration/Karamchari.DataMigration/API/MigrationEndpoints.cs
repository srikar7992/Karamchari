using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Karamchari.Core.Security;
using Karamchari.DataMigration.Domain.Importing;
using Karamchari.DataMigration.Persistence;
using Karamchari.DataMigration.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.DataMigration.API;

public static class MigrationEndpoints
{
    public static void MapMigrationEndpoints(this IEndpointRouteBuilder app)
    {
        var imports = app.MapGroup("/api/v1/migration/imports")
            .WithTags("DataMigration")
            .RequireAuthorization();

        imports.MapPost("/", UploadImport)
            .RequireAuthorization(Permissions.BulkImportCreate)
            .WithName("UploadImport")
            .DisableAntiforgery();

        imports.MapGet("/{id}", GetImportStatus)
            .RequireAuthorization(Permissions.BulkImportRead)
            .WithName("GetImportStatus");

        imports.MapGet("/{id}/preview", GetImportPreview)
            .RequireAuthorization(Permissions.BulkImportRead)
            .WithName("GetImportPreview");

        imports.MapPost("/{id}/validate", ValidateImport)
            .RequireAuthorization(Permissions.BulkImportCreate)
            .WithName("ValidateImport");

        imports.MapPost("/{id}/execute", ExecuteImport)
            .RequireAuthorization(Permissions.BulkImportExecute)
            .WithName("ExecuteImport");

        imports.MapPost("/{id}/cancel", CancelImport)
            .RequireAuthorization(Permissions.BulkImportCancel)
            .WithName("CancelImport");

        imports.MapGet("/{id}/error-report", DownloadErrorReport)
            .RequireAuthorization(Permissions.BulkImportRead)
            .WithName("DownloadImportErrorReport");
    }

    private static async Task<IResult> UploadImport(
        [FromForm] string importType,
        [FromForm] string? templateVersion,
        [FromForm] string? conflictPolicy,
        IFormFile file,
        ClaimsPrincipal user,
        IImportFileStore fileStore,
        DataMigrationDbContext db)
    {
        if (file == null || file.Length == 0) return Results.BadRequest("No file uploaded.");

        var policy = Enum.TryParse<ImportConflictPolicy>(conflictPolicy, true, out var p) ? p : ImportConflictPolicy.SkipExisting;

        using var stream = file.OpenReadStream();
        using var sha = SHA256.Create();
        var hashBytes = await sha.ComputeHashAsync(stream);
        var fileHash = Convert.ToHexStringLower(hashBytes);
        stream.Position = 0;

        var fileId = await fileStore.SaveFileAsync(stream, file.FileName);

        var job = ImportJob.Create(
            importType,
            file.FileName,
            fileId,
            fileHash,
            user.Identity?.Name ?? "system",
            policy,
            templateVersion ?? "1.0");

        db.ImportJobs.Add(job);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/migration/imports/{job.Id}", new { job.Id });
    }

    private static async Task<IResult> GetImportStatus(Guid id, DataMigrationDbContext db)
    {
        var job = await db.ImportJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id);
        if (job is null) return Results.NotFound();
        return Results.Ok(job);
    }

    private static async Task<IResult> GetImportPreview(
        Guid id,
        DataMigrationDbContext db,
        IImportFileStore fileStore,
        IEnumerable<IImportFileParser> parsers,
        IEnumerable<IImportPipeline> pipelines)
    {
        var job = await db.ImportJobs.FindAsync(id);
        if (job is null) return Results.NotFound();

        var pipeline = pipelines.FirstOrDefault(p => p.ImportType == job.ImportType);
        if (pipeline is null) return Results.Problem($"No pipeline registered for import type '{job.ImportType}'.");

        var extension = Path.GetExtension(job.FileName);
        var parser = parsers.FirstOrDefault(p => p.SupportedExtension.Equals(extension, StringComparison.OrdinalIgnoreCase));
        if (parser is null) return Results.Problem($"No parser for file extension '{extension}'.");

        using var stream = await fileStore.GetFileAsync(job.StoredFileId);
        var rows = await pipeline.PreviewAsync(stream, parser);

        return Results.Ok(new ImportPreviewResult(job.Id, job.ImportType, rows.Count(), rows));
    }

    private static async Task<IResult> ValidateImport(
        Guid id,
        DataMigrationDbContext db,
        IImportFileStore fileStore,
        IEnumerable<IImportFileParser> parsers,
        IEnumerable<IImportPipeline> pipelines)
    {
        var job = await db.ImportJobs.FindAsync(id);
        if (job is null) return Results.NotFound();

        var pipeline = pipelines.FirstOrDefault(p => p.ImportType == job.ImportType);
        if (pipeline is null) return Results.Problem($"No pipeline registered for import type '{job.ImportType}'.");

        var extension = Path.GetExtension(job.FileName);
        var parser = parsers.FirstOrDefault(p => p.SupportedExtension.Equals(extension, StringComparison.OrdinalIgnoreCase));
        if (parser is null) return Results.Problem($"No parser for file extension '{extension}'.");

        using var stream = await fileStore.GetFileAsync(job.StoredFileId);
        var (valid, invalid, errors) = await pipeline.ValidateAsync(stream, parser);

        job.TransitionTo(errors.Count > 0 ? ImportJobStatus.ValidationFailed : ImportJobStatus.Validated);
        job.UpdateProgress(valid, invalid, 0);
        await db.SaveChangesAsync();

        return Results.Ok(new ImportValidationSummary(job.Id, valid, invalid, errors.Take(100)));
    }

    private static async Task<IResult> ExecuteImport(
        Guid id,
        DataMigrationDbContext db,
        MassTransit.IPublishEndpoint publishEndpoint)
    {
        var job = await db.ImportJobs.FindAsync(id);
        if (job is null) return Results.NotFound();

        if (job.Status != ImportJobStatus.Validated && job.Status != ImportJobStatus.CompletedWithErrors)
            return Results.BadRequest($"Job must be Validated to execute. Current status: {job.Status}");

        job.TransitionTo(ImportJobStatus.Queued);
        await db.SaveChangesAsync();

        await publishEndpoint.Publish(new Contracts.Events.ImportJobQueued(job.Id, job.TenantId, job.ImportType));

        return Results.Accepted($"/api/v1/migration/imports/{job.Id}", new { job.Id, job.Status });
    }

    private static async Task<IResult> CancelImport(Guid id, DataMigrationDbContext db)
    {
        var job = await db.ImportJobs.FindAsync(id);
        if (job is null) return Results.NotFound();

        if (job.Status is ImportJobStatus.Completed or ImportJobStatus.Failed or ImportJobStatus.Cancelled)
            return Results.BadRequest($"Cannot cancel a job in status '{job.Status}'.");

        job.TransitionTo(ImportJobStatus.Cancelled);
        await db.SaveChangesAsync();

        return Results.Ok(new { job.Id, job.Status });
    }

    private static async Task<IResult> DownloadErrorReport(Guid id, DataMigrationDbContext db)
    {
        var job = await db.ImportJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id);
        if (job is null) return Results.NotFound();

        var failedRecords = await db.ImportRecords
            .AsNoTracking()
            .Where(r => r.ImportJobId == id && r.Status == ImportRecordStatus.Failed)
            .OrderBy(r => r.RowNumber)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("RowNumber,ErrorMessage");
        foreach (var record in failedRecords)
        {
            var safeError = (record.ErrorMessage ?? "Unknown error").Replace("\"", "\"\"");
            csv.AppendLine($"{record.RowNumber},\"{safeError}\"");
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return Results.File(bytes, "text/csv", $"import-errors-{id}.csv");
    }
}
