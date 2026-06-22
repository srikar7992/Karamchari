// -----------------------------------------------------------------------
// <copyright file="ComplianceReport.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain.CountryEngine.Results;

/// <summary>
/// A generated statutory compliance report for a single obligation in a single period.
/// </summary>
public sealed record ComplianceReport(
    /// <summary>Matches a <see cref="ComplianceObligation.Code"/> from the reporting calendar.</summary>
    string ReportCode,

    string DisplayName,
    ComplianceReportFormat Format,

    /// <summary>Generated content for text-based formats (fixed-width, CSV). Null for binary formats.</summary>
    string? TextContent,

    /// <summary>Generated content for binary formats (Excel, PDF). Null for text formats.</summary>
    byte[]? BinaryContent,

    DateOnly DueDate
);

/// <summary>The serialization format of the generated compliance report.</summary>
public enum ComplianceReportFormat
{
    FixedWidth = 1,
    Csv = 2,
    Excel = 3,
    Pdf = 4
}
