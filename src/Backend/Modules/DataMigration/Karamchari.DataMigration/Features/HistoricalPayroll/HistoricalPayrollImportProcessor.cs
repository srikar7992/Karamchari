using Karamchari.DataMigration.Domain.Importing;
using Karamchari.DataMigration.Persistence;
using Karamchari.DataMigration.Services;

namespace Karamchari.DataMigration.Features.HistoricalPayroll;

public sealed class HistoricalPayrollImportProcessor : IImportProcessor<HistoricalPayrollImportRecord>
{
    private readonly DataMigrationDbContext _db;

    public HistoricalPayrollImportProcessor(DataMigrationDbContext db)
    {
        _db = db;
    }

    public string ImportType => "HistoricalPayrollImport";

    public async Task ProcessBatchAsync(IEnumerable<HistoricalPayrollImportRecord> items, CancellationToken ct = default)
    {
        foreach (var item in items)
        {
            var summary = HistoricalPayrollSummary.Create(
                importJobId: Guid.Empty, // populated by consumer from job context
                employeeNumber: item.EmployeeNumber!,
                month: item.Month!.Value,
                year: item.Year!.Value,
                grossPay: item.GrossPay ?? 0m,
                deductions: item.Deductions ?? 0m,
                netPay: item.NetPay!.Value);

            _db.HistoricalPayrollSummaries.Add(summary);
        }

        await _db.SaveChangesAsync(ct);
    }
}
