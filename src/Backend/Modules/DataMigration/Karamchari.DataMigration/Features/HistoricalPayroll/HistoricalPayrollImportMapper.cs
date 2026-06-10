// -----------------------------------------------------------------------
// <copyright file="HistoricalPayrollImportMapper.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.DataMigration.Services;

namespace Karamchari.DataMigration.Features.HistoricalPayroll;

public sealed class HistoricalPayrollImportMapper : IImportMapper<HistoricalPayrollImportRecord>
{
    public HistoricalPayrollImportRecord Map(ImportRow row) => new()
    {
        EmployeeNumber = row.Data.GetValueOrDefault("employee number") ?? row.Data.GetValueOrDefault("emp id"),
        Month = int.TryParse(row.Data.GetValueOrDefault("month"), out var m) ? m : null,
        Year = int.TryParse(row.Data.GetValueOrDefault("year"), out var y) ? y : null,
        GrossPay = decimal.TryParse(row.Data.GetValueOrDefault("gross pay") ?? row.Data.GetValueOrDefault("gross"), out var gp) ? gp : null,
        Deductions = decimal.TryParse(row.Data.GetValueOrDefault("deductions") ?? row.Data.GetValueOrDefault("total deductions"), out var ded) ? ded : null,
        NetPay = decimal.TryParse(row.Data.GetValueOrDefault("net pay") ?? row.Data.GetValueOrDefault("net"), out var np) ? np : null
    };
}
