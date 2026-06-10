// -----------------------------------------------------------------------
// <copyright file="ImportJobStatus.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.DataMigration.Domain.Importing;

/// <summary>
/// Defines the lifecycle states of an import job.
/// </summary>
public enum ImportJobStatus
{
    /// <summary>Job created, file uploaded.</summary>
    Created,

    /// <summary>Sample rows extracted and mapped for user review.</summary>
    Previewed,

    /// <summary>Entire file passed all validation layers (Schema, Mapping, Business).</summary>
    Validated,

    /// <summary>Validation failed for one or more rows.</summary>
    ValidationFailed,

    /// <summary>Job queued for background processing.</summary>
    Queued,

    /// <summary>Worker is currently processing rows.</summary>
    Processing,

    /// <summary>All rows processed successfully.</summary>
    Completed,

    /// <summary>Job finished, but one or more rows failed to import.</summary>
    CompletedWithErrors,

    /// <summary>Catastrophic failure during processing.</summary>
    Failed,

    /// <summary>Job was manually cancelled by an administrator.</summary>
    Cancelled,

    /// <summary>A completed 'Create' import was reverted.</summary>
    RolledBack
}
