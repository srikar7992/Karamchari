// -----------------------------------------------------------------------
// <copyright file="ImportRecord.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.DataMigration.Domain.Importing;

/// <summary>
/// Represents an individual row/record within an import job.
/// </summary>
public sealed class ImportRecord
{
    public Guid Id { get; private set; }
    public Guid ImportJobId { get; private set; }
    public int RowNumber { get; private set; }

    /// <summary>The original raw row data (typically JSON serialized).</summary>
    public string RawPayload { get; private set; } = string.Empty;

    /// <summary>The strongly typed domain payload after mapping.</summary>
    public string MappedPayload { get; private set; } = string.Empty;

    public ImportRecordStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }

    private ImportRecord() { } // EF Core

    public static ImportRecord Create(Guid jobId, int rowNumber, string rawPayload)
    {
        return new ImportRecord
        {
            Id = Guid.NewGuid(),
            ImportJobId = jobId,
            RowNumber = rowNumber,
            RawPayload = rawPayload,
            Status = ImportRecordStatus.Pending
        };
    }

    public void MarkValidated(string mappedPayload)
    {
        MappedPayload = mappedPayload;
        Status = ImportRecordStatus.Validated;
        ErrorMessage = null;
    }

    public void MarkFailed(string error)
    {
        Status = ImportRecordStatus.Failed;
        ErrorMessage = error;
    }

    public void MarkSucceeded()
    {
        Status = ImportRecordStatus.Succeeded;
        ErrorMessage = null;
    }
}

public enum ImportRecordStatus
{
    Pending,
    Validated,
    Succeeded,
    Failed,
    Skipped
}
