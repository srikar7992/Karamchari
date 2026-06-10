// -----------------------------------------------------------------------
// <copyright file="ImportJobQueuedConsumer.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.DataMigration.Contracts.Events;
using Karamchari.DataMigration.Domain.Importing;
using Karamchari.DataMigration.Persistence;
using Karamchari.DataMigration.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Karamchari.DataMigration.Consumers;

public sealed class ImportJobQueuedConsumer : IConsumer<ImportJobQueued>
{
    private readonly DataMigrationDbContext _db;
    private readonly IImportFileStore _fileStore;
    private readonly IEnumerable<IImportFileParser> _parsers;
    private readonly IEnumerable<IImportPipeline> _pipelines;
    private readonly ILogger<ImportJobQueuedConsumer> _logger;

    public ImportJobQueuedConsumer(
        DataMigrationDbContext db,
        IImportFileStore fileStore,
        IEnumerable<IImportFileParser> parsers,
        IEnumerable<IImportPipeline> pipelines,
        ILogger<ImportJobQueuedConsumer> logger)
    {
        _db = db;
        _fileStore = fileStore;
        _parsers = parsers;
        _pipelines = pipelines;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ImportJobQueued> context)
    {
        var job = await _db.ImportJobs.FindAsync(context.Message.JobId);
        if (job is null) return;

        _logger.LogInformation("Processing Import Job {JobId} (Type={Type})", job.Id, job.ImportType);

        job.TransitionTo(ImportJobStatus.Processing);
        await _db.SaveChangesAsync();

        var extension = Path.GetExtension(job.FileName);
        var parser = _parsers.FirstOrDefault(p => p.SupportedExtension.Equals(extension, StringComparison.OrdinalIgnoreCase));
        if (parser is null)
        {
            _logger.LogError("No parser found for extension '{Ext}' in Job {JobId}", extension, job.Id);
            job.TransitionTo(ImportJobStatus.Failed);
            await _db.SaveChangesAsync();
            return;
        }

        var pipeline = _pipelines.FirstOrDefault(p => p.ImportType == job.ImportType);
        if (pipeline is null)
        {
            _logger.LogError("No pipeline registered for import type '{Type}' in Job {JobId}", job.ImportType, job.Id);
            job.TransitionTo(ImportJobStatus.Failed);
            await _db.SaveChangesAsync();
            return;
        }

        try
        {
            using var stream = await _fileStore.GetFileAsync(job.StoredFileId);
            var (success, failed, skipped) = await pipeline.ProcessAsync(stream, parser, job);

            job.UpdateProgress(success, failed, skipped);
            job.TransitionTo(failed > 0 ? ImportJobStatus.CompletedWithErrors : ImportJobStatus.Completed);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Import Job {JobId} completed — {Success} succeeded, {Failed} failed, {Skipped} skipped.",
                job.Id, success, failed, skipped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error processing Import Job {JobId}", job.Id);
            job.TransitionTo(ImportJobStatus.Failed);
            await _db.SaveChangesAsync();
        }
    }
}
