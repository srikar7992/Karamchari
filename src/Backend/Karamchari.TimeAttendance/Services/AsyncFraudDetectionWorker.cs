namespace Karamchari.TimeAttendance.Services;

using Karamchari.TimeAttendance.Persistence;
using Karamchari.TimeAttendance.Domain.IoT;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Scans batches of punches asynchronously to detect complex anomalies
/// like widespread buddy punching or persistent late marks.
/// </summary>
public sealed class AsyncFraudDetectionWorker
{
    private readonly TimeAttendanceDbContext _db;

    public AsyncFraudDetectionWorker(TimeAttendanceDbContext db)
    {
        _db = db;
    }

    public async Task ScanBatchAsync(DateTime dateUtc, CancellationToken ct = default)
    {
        var start = dateUtc.Date;
        var end = start.AddDays(1);

        // 1. Detect Device Sharing (Multiple employees, same device, very rapid succession)
        var suspectGroups = await _db.RawPunches
            .Where(p => p.TimestampUtc >= start && p.TimestampUtc < end)
            .GroupBy(p => new { p.DeviceId, Minute = new DateTime(p.TimestampUtc.Year, p.TimestampUtc.Month, p.TimestampUtc.Day, p.TimestampUtc.Hour, p.TimestampUtc.Minute, 0, DateTimeKind.Utc) })
            .Select(g => new { 
                g.Key.DeviceId, 
                g.Key.Minute, 
                UniqueEmployees = g.Select(x => x.EmployeeId).Distinct().Count(),
                Punches = g.ToList() 
            })
            .Where(x => x.UniqueEmployees > 5) // E.g., 5+ different people on 1 device in 60 seconds
            .ToListAsync(ct);

        foreach (var group in suspectGroups)
        {
            foreach (var punch in group.Punches)
            {
                var exists = await _db.FraudFlags.AnyAsync(f => f.RelatedPunchId == punch.Id, ct);
                if (!exists)
                {
                    var flag = FraudFlag.Create(
                        punch.TenantId,
                        punch.EmployeeId,
                        punch.Id,
                        FraudType.DeviceSharing,
                        "High density punching cluster detected. Possible proxy punching.",
                        60);
                    _db.FraudFlags.Add(flag);
                }
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}
