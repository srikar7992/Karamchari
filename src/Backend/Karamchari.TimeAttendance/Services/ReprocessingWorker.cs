namespace Karamchari.TimeAttendance.Services;

using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Bulk reprocessing worker for historical attendance corrections.
/// Safe to run multiple times due to UPSERT behavior in ShiftReconciliationEngine.
/// </summary>
public sealed class ReprocessingWorker
{
    private readonly TimeAttendanceDbContext _db;
    private readonly ShiftReconciliationEngine _engine;

    public ReprocessingWorker(TimeAttendanceDbContext db, ShiftReconciliationEngine engine)
    {
        _db = db;
        _engine = engine;
    }

    public async Task ReprocessAsync(string tenantId, Guid employeeId, DateTime fromDateUtc, DateTime toDateUtc, CancellationToken ct = default)
    {
        var dates = new List<DateTime>();
        for (var dt = fromDateUtc.Date; dt <= toDateUtc.Date; dt = dt.AddDays(1))
        {
            dates.Add(dt);
        }

        foreach (var date in dates)
        {
            await _engine.ProcessDayAsync(tenantId, employeeId, date, Guid.NewGuid(), ct);
        }
    }
}
