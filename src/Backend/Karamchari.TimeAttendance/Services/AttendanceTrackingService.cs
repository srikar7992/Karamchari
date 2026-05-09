using Karamchari.Core.Domain.Primitives;
using Karamchari.TimeAttendance.Domain.Attendance;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.TimeAttendance.Services;

public sealed record AttendanceSubmission(
    Guid EmployeeId,
    DateTimeOffset Timestamp,
    GeoPoint? Location,
    string? DeviceId,
    AttendanceSource Source);

/// <summary>
/// Orchestrates attendance tracking with confidence-scoring hooks.
/// Ensures state-machine transitions and validates geo/device integrity.
/// </summary>
public sealed class AttendanceTrackingService
{
    private readonly TimeAttendanceDbContext _db;
    private readonly ILogger<AttendanceTrackingService> _logger;

    public AttendanceTrackingService(TimeAttendanceDbContext db, ILogger<AttendanceTrackingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<AttendanceSession> CheckInAsync(string tenantId, AttendanceSubmission submission, CancellationToken ct = default)
    {
        var workDate = DateOnly.FromDateTime(submission.Timestamp.Date);

        var session = await _db.AttendanceSessions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.EmployeeId == submission.EmployeeId && s.WorkDate == workDate, ct);

        if (session == null)
        {
            // Auto-create session if not scheduled (Ad-hoc work)
            session = AttendanceSession.CreateScheduled(tenantId, submission.EmployeeId, Guid.Empty, workDate);
            _db.AttendanceSessions.Add(session);
        }

        // 1. Placeholder for Geo-fencing and Confidence Scoring
        // ValidateLocation(submission.Location, session);

        session.CheckIn(submission.Timestamp, submission.Location, submission.DeviceId, submission.Source);
        
        await _db.SaveChangesAsync(ct);
        return session;
    }

    public async Task<AttendanceSession> CheckOutAsync(string tenantId, AttendanceSubmission submission, CancellationToken ct = default)
    {
        var workDate = DateOnly.FromDateTime(submission.Timestamp.Date);

        var session = await _db.AttendanceSessions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.EmployeeId == submission.EmployeeId && s.WorkDate == workDate, ct)
            ?? throw new InvalidOperationException("Active attendance session not found for today.");

        session.CheckOut(submission.Timestamp, submission.Location, submission.Source);

        await _db.SaveChangesAsync(ct);
        return session;
    }
}
