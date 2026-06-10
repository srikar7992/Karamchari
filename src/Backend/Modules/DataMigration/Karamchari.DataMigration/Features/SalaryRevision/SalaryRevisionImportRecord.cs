// -----------------------------------------------------------------------
// <copyright file="SalaryRevisionImportRecord.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.DataMigration.Features.SalaryRevision;

public record SalaryRevisionImportRecord
{
    public string? EmployeeNumber { get; init; }
    public decimal? PreviousCTC { get; init; }
    public decimal? NewCTC { get; init; }
    public string? EffectiveDate { get; init; }
    public string? RevisionType { get; init; }
    public string? Remarks { get; init; }
}
