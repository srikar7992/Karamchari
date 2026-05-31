namespace Karamchari.DataMigration.Services;

/// <summary>
/// Handles the batched execution and persistence of validated records.
/// </summary>
public interface IImportProcessor<T>
{
    /// <summary>The import type name this processor handles.</summary>
    string ImportType { get; }

    /// <summary>
    /// Processes a batch of records.
    /// </summary>
    Task ProcessBatchAsync(IEnumerable<T> items, CancellationToken ct = default);
}

/// <summary>
/// Centralized utility for resolving external identifiers to domain IDs.
/// </summary>
public interface IReferenceResolver
{
    Task<Guid?> ResolveEmployeeIdAsync(string employeeNumber, CancellationToken ct = default);
    Task<Guid?> ResolveDepartmentIdAsync(string departmentName, CancellationToken ct = default);
    Task<Guid?> ResolveLeavePolicyIdAsync(string policyName, CancellationToken ct = default);
    Task<Guid?> ResolveSalaryComponentIdAsync(string componentName, CancellationToken ct = default);
    Task<string?> ResolveEmployeeNameAsync(Guid employeeId, CancellationToken ct = default);
}
