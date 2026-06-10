// -----------------------------------------------------------------------
// <copyright file="SalaryComponentImportRecord.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.DataMigration.Features.SalaryComponent;

public record SalaryComponentImportRecord
{
    public string? EmployeeNumber { get; init; }
    public string? ComponentName { get; init; }
    public decimal? Amount { get; init; }
    public string? Frequency { get; init; }
}
