// -----------------------------------------------------------------------
// <copyright file="IProofStorage.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Services.Declarations;

/// <summary>
/// Service for storing investment proof documents.
/// </summary>
public interface IProofStorage
{
    /// <summary>
    /// Saves a proof document and returns its unique URI.
    /// </summary>
    Task<string> SaveAsync(Stream stream, string fileName, string tenantId, Guid employeeId, int financialYear);

    /// <summary>
    /// Gets a stream for a stored proof document.
    /// </summary>
    Task<Stream> GetStreamAsync(string proofUri);
}
