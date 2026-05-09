using Karamchari.TimeAttendance.Domain.Shifts;
using Karamchari.TimeAttendance.Domain.Schedules;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.TimeAttendance.Services;

/// <summary>
/// Resolves the active shift for a given employee and date from enterprise rosters.
/// </summary>
public sealed class ShiftRosteringEngine
{
    private readonly TimeAttendanceDbContext _db;

    public ShiftRosteringEngine(TimeAttendanceDbContext db)
    {
        _db = db;
    }

    public async Task<ShiftDefinition?> ResolveShiftAsync(Guid employeeId, DateOnly workDate, CancellationToken ct = default)
    {
        // 1. Resolve from Enterprise Rosters (Workforce_ShiftAssignments)
        var assignment = await _db.WorkforceSchedules
            .SelectMany(s => s.Assignments)
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date == workDate, ct);

        if (assignment == null)
            return null;

        // 2. Load Shift Definition
        return await _db.ShiftDefinitions.FindAsync(new object[] { assignment.ShiftId }, ct);
    }
}
