namespace Karamchari.DataMigration.Domain.Importing;

/// <summary>
/// Defines how the platform handles rows that conflict with existing records.
/// </summary>
public enum ImportConflictPolicy
{
    /// <summary>Do not import if a record with the same identifier already exists.</summary>
    SkipExisting,

    /// <summary>Overwrite existing record with imported values.</summary>
    UpdateExisting,

    /// <summary>Abort the entire import if any record already exists.</summary>
    FailImport,

    /// <summary>Update matching fields, skip non-matching fields (semantic merge).</summary>
    Merge
}
