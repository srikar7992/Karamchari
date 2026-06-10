// -----------------------------------------------------------------------
// <copyright file="IDocumentIntelligenceService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.HR.Services;

/// <summary>
/// Result of an ID document analysis.
/// </summary>
/// <param name="LegalName">The extracted legal name.</param>
/// <param name="DateOfBirth">The extracted date of birth.</param>
/// <param name="IdNumber">The extracted document number.</param>
public record AnalyzedDocumentResult(string LegalName, DateOnly? DateOfBirth, string IdNumber);

/// <summary>
/// Service for analyzing documents using Azure AI Document Intelligence.
/// </summary>
public interface IDocumentIntelligenceService
{
    /// <summary>
    /// Analyzes an ID document stream and extracts key information.
    /// </summary>
    /// <param name="documentStream">The document stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The analyzed document result.</returns>
    Task<AnalyzedDocumentResult> AnalyzeIdDocumentAsync(Stream documentStream, CancellationToken cancellationToken = default);
}
