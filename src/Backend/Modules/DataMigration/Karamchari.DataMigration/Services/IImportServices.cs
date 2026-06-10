// -----------------------------------------------------------------------
// <copyright file="IImportServices.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.DataMigration.Services;

/// <summary>
/// Maps raw row data to a strongly typed domain object.
/// </summary>
public interface IImportMapper<T>
{
    /// <summary>
    /// Maps a row to the target type.
    /// </summary>
    T Map(ImportRow row);
}

/// <summary>
/// Result of a single row validation.
/// </summary>
public record ValidationResult(bool IsValid, string? ErrorMessage = null);

/// <summary>
/// Validates a mapped domain object against business rules and data state.
/// </summary>
public interface IImportValidator<T>
{
    /// <summary>
    /// Validates the object.
    /// </summary>
    Task<ValidationResult> ValidateAsync(T item, CancellationToken ct = default);
}
