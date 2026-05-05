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

    public async Task ReprocessAsync(Guid employeeId, DateTime fromDateUtc, DateTime toDateUtc, CancellationToken ct = default)
    {
        var dates = new List<DateTime>();
        for (var dt = fromDateUtc.Date; dt <= toDateUtc.Date; dt = dt.AddDays(1))
        {
            dates.Add(dt);
        }

        // Processing chronologically guarantees dependencies like continuous shifts are handled properly
        foreach (var date in dates)
        {
            // The engine itself handles idempotent UPSERTs and Audit trailing
            await _engine.ProcessDayAsync(employeeId, date, ct);
        }
    }
}
