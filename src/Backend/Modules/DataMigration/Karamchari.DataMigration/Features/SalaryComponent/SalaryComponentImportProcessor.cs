// -----------------------------------------------------------------------
// <copyright file="SalaryComponentImportProcessor.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.DataMigration.Domain.Importing;
using Karamchari.DataMigration.Persistence;
using Karamchari.DataMigration.Services;

namespace Karamchari.DataMigration.Features.SalaryComponent;

public sealed class SalaryComponentImportProcessor : IImportProcessor<SalaryComponentImportRecord>
{
    private readonly DataMigrationDbContext _db;

    public SalaryComponentImportProcessor(DataMigrationDbContext db)
    {
        _db = db;
    }

    public string ImportType => "SalaryComponentImport";

    public async Task ProcessBatchAsync(IEnumerable<SalaryComponentImportRecord> items, CancellationToken ct = default)
    {
        // Get the current job ID from the first item context.
        // We store historical salary structure data in the DataMigration schema.
        foreach (var item in items)
        {
            var record = HistoricalSalaryRecord.Create(
                Guid.Empty, // populated by consumer from job context
                item.EmployeeNumber!,
                item.ComponentName!,
                item.Amount!.Value,
                item.Frequency ?? "Monthly");

            _db.HistoricalSalaryRecords.Add(record);
        }

        await _db.SaveChangesAsync(ct);
    }
}
