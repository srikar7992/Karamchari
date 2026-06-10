// -----------------------------------------------------------------------
// <copyright file="SalaryRevisionImportMapper.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.DataMigration.Services;

namespace Karamchari.DataMigration.Features.SalaryRevision;

public sealed class SalaryRevisionImportMapper : IImportMapper<SalaryRevisionImportRecord>
{
    public SalaryRevisionImportRecord Map(ImportRow row) => new()
    {
        EmployeeNumber = row.Data.GetValueOrDefault("employee number") ?? row.Data.GetValueOrDefault("emp id"),
        PreviousCTC = decimal.TryParse(row.Data.GetValueOrDefault("previous ctc") ?? row.Data.GetValueOrDefault("old ctc"), out var prev) ? prev : null,
        NewCTC = decimal.TryParse(row.Data.GetValueOrDefault("new ctc") ?? row.Data.GetValueOrDefault("ctc"), out var nw) ? nw : null,
        EffectiveDate = row.Data.GetValueOrDefault("effective date") ?? row.Data.GetValueOrDefault("date"),
        RevisionType = row.Data.GetValueOrDefault("revision type") ?? row.Data.GetValueOrDefault("type"),
        Remarks = row.Data.GetValueOrDefault("remarks") ?? row.Data.GetValueOrDefault("notes")
    };
}
