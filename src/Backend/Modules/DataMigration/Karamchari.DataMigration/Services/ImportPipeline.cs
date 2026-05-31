using System.Text.Json;
using Karamchari.DataMigration.API;
using Karamchari.DataMigration.Domain.Importing;
using Karamchari.DataMigration.Persistence;
using Microsoft.Extensions.Logging;

namespace Karamchari.DataMigration.Services;

/// <summary>
/// Generic implementation of <see cref="IImportPipeline"/> that wires together the
/// mapper, validator, and processor for a specific record type T.
/// </summary>
public sealed class ImportPipeline<T> : IImportPipeline
{
    private readonly IImportMapper<T> _mapper;
    private readonly IImportValidator<T> _validator;
    private readonly IImportProcessor<T> _processor;
    private readonly ILogger<ImportPipeline<T>> _logger;

    public ImportPipeline(
        IImportMapper<T> mapper,
        IImportValidator<T> validator,
        IImportProcessor<T> processor,
        ILogger<ImportPipeline<T>> logger)
    {
        _mapper = mapper;
        _validator = validator;
        _processor = processor;
        _logger = logger;
    }

    public string ImportType => _processor.ImportType;

    public async Task<IEnumerable<object>> PreviewAsync(
        Stream stream,
        IImportFileParser parser,
        int maxRows = 10,
        CancellationToken ct = default)
    {
        var rows = new List<object>();
        await foreach (var raw in parser.ParseAsync(stream).Take(maxRows).WithCancellation(ct))
        {
            rows.Add(_mapper.Map(raw)!);
        }
        return rows;
    }

    public async Task<(int validCount, int invalidCount, List<RowError> errors)> ValidateAsync(
        Stream stream,
        IImportFileParser parser,
        CancellationToken ct = default)
    {
        var errors = new List<RowError>();
        var valid = 0;
        var invalid = 0;

        await foreach (var raw in parser.ParseAsync(stream).WithCancellation(ct))
        {
            try
            {
                var mapped = _mapper.Map(raw);
                var result = await _validator.ValidateAsync(mapped, ct);
                if (result.IsValid)
                    valid++;
                else
                {
                    invalid++;
                    errors.Add(new RowError(raw.RowNumber, result.ErrorMessage ?? "Validation failed."));
                }
            }
            catch (Exception ex)
            {
                invalid++;
                errors.Add(new RowError(raw.RowNumber, $"Mapping error: {ex.Message}"));
            }
        }

        return (valid, invalid, errors);
    }

    public async Task<(int success, int failed, int skipped)> ProcessAsync(
        Stream stream,
        IImportFileParser parser,
        ImportJob job,
        CancellationToken ct = default)
    {
        var success = 0;
        var failed = 0;
        var skipped = 0;
        var batch = new List<T>();
        const int batchSize = 500;

        await foreach (var raw in parser.ParseAsync(stream).WithCancellation(ct))
        {
            try
            {
                var mapped = _mapper.Map(raw);
                var validation = await _validator.ValidateAsync(mapped, ct);

                if (validation.IsValid)
                {
                    batch.Add(mapped);

                    if (batch.Count >= batchSize)
                    {
                        await _processor.ProcessBatchAsync(batch, ct);
                        success += batch.Count;
                        batch.Clear();
                    }
                }
                else
                {
                    failed++;
                    _logger.LogDebug(
                        "Row {Row} failed validation in job {JobId}: {Error}",
                        raw.RowNumber, job.Id, validation.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(ex, "Error processing row {Row} in job {JobId}", raw.RowNumber, job.Id);
            }
        }

        if (batch.Count > 0)
        {
            await _processor.ProcessBatchAsync(batch, ct);
            success += batch.Count;
        }

        return (success, failed, skipped);
    }
}
