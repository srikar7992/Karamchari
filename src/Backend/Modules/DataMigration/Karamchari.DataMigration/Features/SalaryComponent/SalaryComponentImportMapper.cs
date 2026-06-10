// -----------------------------------------------------------------------
// <copyright file="SalaryComponentImportMapper.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.DataMigration.Services;

namespace Karamchari.DataMigration.Features.SalaryComponent;

public sealed class SalaryComponentImportMapper : IImportMapper<SalaryComponentImportRecord>
{
    public SalaryComponentImportRecord Map(ImportRow row) => new()
    {
        EmployeeNumber = row.Data.GetValueOrDefault("employee number") ?? row.Data.GetValueOrDefault("emp id"),
        ComponentName = row.Data.GetValueOrDefault("component") ?? row.Data.GetValueOrDefault("component name"),
        Amount = decimal.TryParse(row.Data.GetValueOrDefault("amount"), out var a) ? a : null,
        Frequency = row.Data.GetValueOrDefault("frequency") ?? row.Data.GetValueOrDefault("pay frequency")
    };
}
