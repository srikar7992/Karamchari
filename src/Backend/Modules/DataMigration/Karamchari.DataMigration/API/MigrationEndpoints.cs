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
        [FromServices] DataMigrationDbContext db)
    {
        if (file == null || file.Length == 0) return Results.BadRequest("No file uploaded.");

        var policy = Enum.TryParse<ImportConflictPolicy>(conflictPolicy, true, out var p) ? p : ImportConflictPolicy.SkipExisting;

        using var stream = file.OpenReadStream();
        using var sha = SHA256.Create();
        var hashBytes = await sha.ComputeHashAsync(stream);
        var fileHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var storedFileId = await fileStore.SaveFileAsync(stream, file.FileName);
        
        var job = ImportJob.Create(
            importType,
            file.FileName,
            storedFileId,
            fileHash,
            user.Identity?.Name ?? "system",
            policy);

        db.ImportJobs.Add(job);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/migration/imports/{job.Id}", new { id = job.Id });
    }

    private static async Task<IResult> GetImportStatus(Guid id, [FromServices] DataMigrationDbContext db)
    {
        var job = await db.ImportJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        return job == null ? Results.NotFound() : Results.Ok(job);
    }

    private static async Task<IResult> GetImportPreview(
        Guid id,
        [FromServices] DataMigrationDbContext db,
        [FromQuery] int limit = 10)
    {
        var records = await db.ImportRecords
            .AsNoTracking()
            .Where(x => x.ImportJobId == id)
            .Take(limit)
            .ToListAsync();

        return Results.Ok(records);
    }

    private static async Task<IResult> ValidateImport(
        Guid id,
        [FromServices] DataMigrationDbContext db,
        [FromServices] MassTransit.IPublishEndpoint publishEndpoint)
    {
        var job = await db.ImportJobs.FirstOrDefaultAsync(x => x.Id == id);
        if (job == null) return Results.NotFound();

        if (job.Status != ImportJobStatus.Created)
            return Results.BadRequest("Only newly created jobs can be validated.");

        // Handled by background worker
        await publishEndpoint.Publish(new Contracts.Events.ImportJobQueued(job.Id, job.TenantId, job.ImportType));

        return Results.Accepted();
    }

    private static async Task<IResult> ExecuteImport(
        Guid id,
        [FromServices] DataMigrationDbContext db,
        [FromServices] MassTransit.IPublishEndpoint publishEndpoint)
    {
        var job = await db.ImportJobs.FirstOrDefaultAsync(x => x.Id == id);
        if (job == null) return Results.NotFound();

        if (job.Status != ImportJobStatus.Validated)
            return Results.BadRequest("Only validated jobs can be executed.");

        job.TransitionTo(ImportJobStatus.Processing);
        await db.SaveChangesAsync();

        // Handled by background worker
        await publishEndpoint.Publish(new Contracts.Events.ImportJobQueued(job.Id, job.TenantId, job.ImportType));

        return Results.Accepted();
    }

    private static async Task<IResult> CancelImport(Guid id, [FromServices] DataMigrationDbContext db)
    {
        var job = await db.ImportJobs.FirstOrDefaultAsync(x => x.Id == id);
        if (job == null) return Results.NotFound();

        job.TransitionTo(ImportJobStatus.Cancelled);
        await db.SaveChangesAsync();

        return Results.Ok(job);
    }

    private static async Task<IResult> DownloadErrorReport(Guid id, [FromServices] DataMigrationDbContext db)
    {
        var records = await db.ImportRecords
            .AsNoTracking()
            .Where(x => x.ImportJobId == id && x.Status == ImportRecordStatus.Failed)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("RowNumber,ErrorMessage");
        foreach (var record in records)
        {
            var safeError = record.ErrorMessage?.Replace("\"", "\"\"");
            csv.AppendLine($"{record.RowNumber},\"{safeError}\"");
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return Results.File(bytes, "text/csv", $"import-errors-{id}.csv");
    }
}
