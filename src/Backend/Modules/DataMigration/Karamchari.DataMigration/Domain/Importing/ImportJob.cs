// -----------------------------------------------------------------------
// <copyright file="ImportJob.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;

namespace Karamchari.DataMigration.Domain.Importing;

/// <summary>
/// Represents a bulk data migration job for a specific tenant.
/// </summary>
public sealed class ImportJob : ITenantOwned
{
    private readonly List<ImportRecord> _records = new();

    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public string ImportType { get; private set; } = string.Empty;
    public string TemplateVersion { get; private set; } = "1.0";
    public string FileHash { get; private set; } = string.Empty;
    public string StoredFileId { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public ImportConflictPolicy ConflictPolicy { get; private set; }
    public ImportJobStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;

    public int TotalRows { get; private set; }
    public int SuccessfulRows { get; private set; }
    public int FailedRows { get; private set; }
    public int SkippedRows { get; private set; }

    public IReadOnlyCollection<ImportRecord> Records => _records.AsReadOnly();

    private ImportJob() { } // EF Core

    public static ImportJob Create(
        string importType,
        string fileName,
        string storedFileId,
        string fileHash,
        string createdBy,
        ImportConflictPolicy conflictPolicy = ImportConflictPolicy.SkipExisting,
        string templateVersion = "1.0")
    {
        return new ImportJob
        {
            Id = Guid.NewGuid(),
            ImportType = importType,
            FileName = fileName,
            StoredFileId = storedFileId,
            FileHash = fileHash,
            CreatedBy = createdBy,
            ConflictPolicy = conflictPolicy,
            TemplateVersion = templateVersion,
            Status = ImportJobStatus.Created,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void TransitionTo(ImportJobStatus newStatus)
    {
        // Simple state machine validation
        if (Status == ImportJobStatus.Completed || Status == ImportJobStatus.Failed || Status == ImportJobStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot transition from {Status} to {newStatus}.");
        }

        Status = newStatus;
        if (newStatus is ImportJobStatus.Completed or ImportJobStatus.CompletedWithErrors or ImportJobStatus.Failed or ImportJobStatus.Cancelled)
        {
            CompletedAt = DateTime.UtcNow;
        }
    }

    public void UpdateProgress(int success, int failed, int skipped)
    {
        SuccessfulRows = success;
        FailedRows = failed;
        SkippedRows = skipped;
        TotalRows = success + failed + skipped;
    }
}
