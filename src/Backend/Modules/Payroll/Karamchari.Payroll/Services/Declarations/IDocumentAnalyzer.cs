// -----------------------------------------------------------------------
// <copyright file="IDocumentAnalyzer.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Services.Declarations;

/// <summary>
/// Result of analyzing a tax proof document.
/// </summary>
public record DocumentAnalysisResult
{
    /// <summary>Gets the inferred category (e.g., 80C, 80D, HRA).</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>Gets the extracted amount.</summary>
    public decimal? ExtractedAmount { get; init; }

    /// <summary>Gets the document date.</summary>
    public DateTime? DocumentDate { get; init; }

    /// <summary>Gets the vendor or issuer name.</summary>
    public string? Vendor { get; init; }

    /// <summary>Gets the confidence score (0-1).</summary>
    public float Confidence { get; init; }

    /// <summary>Gets the URI where the document is stored.</summary>
    public string StorageUri { get; init; } = string.Empty;

    /// <summary>Gets a value indicating whether the document requires manual HR review.</summary>
    public bool RequiresReview => Confidence < 0.85f;
}

/// <summary>
/// Service for analyzing investment proof documents using AI.
/// </summary>
public interface IDocumentAnalyzer
{
    /// <summary>
    /// Analyzes a document from a stream.
    /// </summary>
    Task<DocumentAnalysisResult> AnalyzeAsync(Stream stream, string fileName);
}
