using Karamchari.TimeAttendance.Domain.Attendance;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.TimeAttendance.Services;

/// <summary>
/// Absence pattern analysis — historical trend, not prediction.
/// "This employee is absent 40% of Mondays" is a trend observation.
/// Label it as such, not as a forecast. Predictions require ML or Bayesian models, not averages.
/// </summary>
public sealed class AbsenceTrendAnalyzer(TimeAttendanceDbContext db)
{
    private readonly TimeAttendanceDbContext _db = db;

    /// <summary>
    /// Returns per-day-of-week historical absence rate for an employee.
    /// This is a trend observation over the lookback window, not a prediction of future behavior.
    /// </summary>
    public async Task<IDictionary<DayOfWeek, decimal>> GetAbsenceTrendByDayAsync(
        string tenantId,
        Guid employeeId,
        int lookbackDays = 90,
        CancellationToken ct = default)
    {
        var since = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-lookbackDays));

        List<AttendanceRecord> records = await _db.AttendanceRecords
            .Where(r =>
                r.TenantId == tenantId &&
                r.EmployeeId == employeeId &&
                r.WorkDate >= since &&
                r.Status != AttendanceStatus.Pending &&
                r.Status != AttendanceStatus.OnLeave &&
                r.Status != AttendanceStatus.Holiday &&
                r.Status != AttendanceStatus.WeekOff)
            .ToListAsync(ct);

        var result = new Dictionary<DayOfWeek, decimal>();
        foreach (IGrouping<DayOfWeek, AttendanceRecord> dayGroup in records.GroupBy(r => r.WorkDate.DayOfWeek))
        {
            int total = dayGroup.Count();
            int absent = dayGroup.Count(r => r.Status == AttendanceStatus.Absent);
            result[dayGroup.Key] = total == 0 ? 0m : Math.Round((decimal)absent / total * 100, 1);
        }
        return result;
    }
}
