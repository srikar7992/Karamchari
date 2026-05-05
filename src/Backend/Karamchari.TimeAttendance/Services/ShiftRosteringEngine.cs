namespace Karamchari.TimeAttendance.Services;

using Karamchari.TimeAttendance.Domain.Shifts;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Resolves the active shift for a given employee and date, taking into account
/// overrides, weekly offs, and complex rotations.
/// </summary>
public sealed class ShiftRosteringEngine
{
    private readonly TimeAttendanceDbContext _db;

    public ShiftRosteringEngine(TimeAttendanceDbContext db)
    {
        _db = db;
    }

    public async Task<ShiftTemplate?> ResolveShiftAsync(Guid employeeId, DateTime dateUtc, CancellationToken ct = default)
    {
        var localDate = dateUtc.Date; // Simplification: assuming DB date fields match local date

        // 1. Check Ad-hoc Override
        var overrideRecord = await _db.ShiftOverrides
            .FirstOrDefaultAsync(o => o.EmployeeId == employeeId && o.Date == localDate, ct);
            
        if (overrideRecord != null)
        {
            return await _db.ShiftTemplates.FindAsync(new object[] { overrideRecord.ShiftTemplateId }, ct);
        }

        // 2. Check Weekly Off
        var weekOfMonth = (localDate.Day - 1) / 7 + 1;
        var offRules = await _db.WeeklyOffRules
            .Where(w => w.EmployeeId == employeeId && w.Day == localDate.DayOfWeek)
            .ToListAsync(ct);

        foreach (var rule in offRules)
        {
            if (!rule.IsAlternateWeek)
                return null; // Regular weekly off
                
            if (rule.IsAlternateWeek && weekOfMonth % 2 == 0)
                return null; // Alternate week off matched
        }

        // 3. Resolve Active Assignment & Rotation
        var assignment = await _db.ShiftAssignments
            .Where(a => a.EmployeeId == employeeId && a.EffectiveFrom <= localDate && (a.EffectiveTo == null || a.EffectiveTo >= localDate))
            .OrderByDescending(a => a.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        if (assignment == null)
            return null;

        if (!assignment.IsRotation)
        {
            return await _db.ShiftTemplates.FindAsync(new object[] { assignment.ShiftTemplateId }, ct);
        }

        // Handle rotation logic (e.g. 7 day cycle)
        var daysSinceStart = (localDate - assignment.EffectiveFrom.Date).Days;
        var cycleIndex = daysSinceStart % assignment.RotationCycleDays;
        
        // Note: For full rotation support, we would have a RotationPattern map.
        // Assuming assignment.ShiftTemplateId points to the base template for now.
        return await _db.ShiftTemplates.FindAsync(new object[] { assignment.ShiftTemplateId }, ct);
    }
}
