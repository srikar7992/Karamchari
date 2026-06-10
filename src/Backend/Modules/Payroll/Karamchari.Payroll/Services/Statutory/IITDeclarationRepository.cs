// -----------------------------------------------------------------------
// <copyright file="IITDeclarationRepository.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Services.Statutory;

using Karamchari.Payroll.Domain.Statutory;

/// <summary>
/// Defines a repository for managing and fetching investment declarations.
/// </summary>
public interface IITDeclarationRepository
{
    /// <summary>
    /// Gets ALL declarations that have been approved by HR for the specific employee and year.
    /// This is the primary method used by the TDS engine.
    /// </summary>
    Task<IReadOnlyList<ITDeclaration>> GetApprovedDeclarationsAsync(Guid employeeId, int financialYear);

    /// <summary>
    /// Gets the most recent declaration for an employee (regardless of status).
    /// Used by the ESS portal for display/edit.
    /// </summary>
    Task<ITDeclaration?> GetLatestAsync(Guid employeeId, int financialYear, string category);

    /// <summary>
    /// Gets a list of all declarations pending HR review across the tenant.
    /// </summary>
    Task<List<ITDeclaration>> GetPendingReviewAsync();

    /// <summary>
    /// Gets a declaration by its unique ID.
    /// </summary>
    Task<ITDeclaration?> GetByIdAsync(Guid id);

    /// <summary>
    /// Adds or updates a declaration.
    /// </summary>
    Task UpsertAsync(ITDeclaration declaration);

    /// <summary>
    /// Persists changes to the database.
    /// </summary>
    Task SaveChangesAsync();
}
