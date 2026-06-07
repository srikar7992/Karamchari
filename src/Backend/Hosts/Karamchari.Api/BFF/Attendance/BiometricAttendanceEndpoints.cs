using System.Security.Claims;
using Karamchari.Api.BFF.Common;
using Karamchari.TimeAttendance.Domain.Biometric;
using Karamchari.TimeAttendance.Domain.Geo;
using Karamchari.TimeAttendance.Domain.Shifts;
using Karamchari.TimeAttendance.Persistence;
using Karamchari.TimeAttendance.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Attendance;

public static class BiometricAttendanceEndpoints
{
    public static WebApplication MapBiometricAttendanceEndpoints(this WebApplication app)
    {
        var punch = app.MapGroup("/api/v1/attendance/punch").RequireAuthorization();
        punch.MapPost("/", IngestPunchAsync)
            .WithName("Attendance.Punch.Ingest")
            .WithTags("Attendance", "Biometric");

        var geo = app.MapGroup("/api/v1/attendance/geo-zones").RequireAuthorization();
        geo.MapGet("/", ListGeoZonesAsync).WithName("Geo.Zone.List");
        geo.MapGet("/{id:guid}", GetGeoZoneAsync).WithName("Geo.Zone.Get");
        geo.MapPost("/radial", CreateRadialZoneAsync).WithName("Geo.Zone.CreateRadial");
        geo.MapPost("/polygon", CreatePolygonZoneAsync).WithName("Geo.Zone.CreatePolygon");
        geo.MapPut("/{id:guid}/activate", ActivateZoneAsync).WithName("Geo.Zone.Activate");
        geo.MapPut("/{id:guid}/deactivate", DeactivateZoneAsync).WithName("Geo.Zone.Deactivate");

        var enrollment = app.MapGroup("/api/v1/attendance/enrollment").RequireAuthorization();
        enrollment.MapPost("/sessions", StartEnrollmentSessionAsync).WithName("Biometric.Enrollment.Start");
        enrollment.MapPost("/sessions/{id:guid}/consent", RecordConsentAsync).WithName("Biometric.Enrollment.Consent");
        enrollment.MapPost("/sessions/{id:guid}/sample", RecordSampleAsync).WithName("Biometric.Enrollment.Sample");
        enrollment.MapPost("/sessions/{id:guid}/complete", CompleteEnrollmentAsync).WithName("Biometric.Enrollment.Complete");
        enrollment.MapPost("/sessions/{id:guid}/cancel", CancelEnrollmentAsync).WithName("Biometric.Enrollment.Cancel");
        enrollment.MapGet("/sessions/{id:guid}", GetEnrollmentSessionAsync).WithName("Biometric.Enrollment.Get");

        var templates = app.MapGroup("/api/v1/attendance/biometric-templates").RequireAuthorization();
        templates.MapGet("/employee/{employeeId:guid}", GetEmployeeTemplatesAsync).WithName("Biometric.Template.List");
        templates.MapPost("/{id:guid}/revoke", RevokeTemplateAsync).WithName("Biometric.Template.Revoke");
        templates.MapPost("/{id:guid}/authorize-device", AuthorizeDeviceAsync).WithName("Biometric.Template.AuthorizeDevice");

        var devices = app.MapGroup("/api/v1/attendance/biometric-devices").RequireAuthorization();
        devices.MapGet("/", ListDevicesAsync).WithName("Biometric.Device.List");
        devices.MapPost("/", RegisterDeviceAsync).WithName("Biometric.Device.Register");
        devices.MapPost("/{id:guid}/activate", ActivateDeviceAsync).WithName("Biometric.Device.Activate");
        devices.MapPost("/{id:guid}/decommission", DecommissionDeviceAsync).WithName("Biometric.Device.Decommission");
        devices.MapPost("/{id:guid}/heartbeat", RecordHeartbeatAsync).WithName("Biometric.Device.Heartbeat");

        var shiftDefs = app.MapGroup("/api/v1/attendance/shift-definitions").RequireAuthorization();
        shiftDefs.MapGet("/", ListShiftDefinitionsAsync).WithName("Shift.Definition.List");
        shiftDefs.MapPost("/fixed", CreateFixedShiftAsync).WithName("Shift.Definition.CreateFixed");
        shiftDefs.MapPost("/flexible", CreateFlexibleShiftAsync).WithName("Shift.Definition.CreateFlexible");
        shiftDefs.MapPost("/rotating", CreateRotatingShiftAsync).WithName("Shift.Definition.CreateRotating");
        shiftDefs.MapPost("/split", CreateSplitShiftAsync).WithName("Shift.Definition.CreateSplit");
        shiftDefs.MapPost("/{id:guid}/deactivate", DeactivateShiftDefinitionAsync).WithName("Shift.Definition.Deactivate");

        var finalization = app.MapGroup("/api/v1/attendance/period-finalization").RequireAuthorization();
        finalization.MapPost("/{year:int}/{month:int}", FinalizeAttendancePeriodAsync).WithName("Attendance.Period.Finalize");

        return app;
    }

    // ─── Punch ───────────────────────────────────────────────────────────────

    private static async Task<IResult> IngestPunchAsync(
        PunchIngestionRequest body,
        PunchIngestionService ingestionService,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        var direction = body.Direction.Equals("out", StringComparison.OrdinalIgnoreCase)
            ? PunchDirection.Out
            : PunchDirection.In;

        var captureMode = Enum.TryParse<CaptureMode>(body.CaptureMode, ignoreCase: true, out var cm)
            ? cm
            : CaptureMode.Mobile;

        var modality = body.Modality is not null && Enum.TryParse<BiometricModality>(body.Modality, ignoreCase: true, out var mo)
            ? (BiometricModality?)mo
            : null;

        var request = new PunchIngestionService.PunchRequest(
            tenantId,
            employeeId.Value,
            body.DeviceId,
            body.DeviceTimestampUtc ?? DateTimeOffset.UtcNow,
            direction,
            captureMode,
            modality,
            body.Latitude,
            body.Longitude,
            body.GpsAccuracyMeters,
            body.WifiSsidMatched,
            body.IpWhitelisted,
            body.BleBeaconDetected,
            body.VpnMatched,
            body.MockGpsDetected,
            body.JailbreakDetected,
            body.DeviceFingerprint,
            body.BiometricMatchScore,
            body.IdempotencyKey ?? Guid.NewGuid().ToString());

        var result = await ingestionService.IngestAsync(request, ct);

        return result.Accepted
            ? Results.Ok(new
            {
                accepted = true,
                punchEventId = result.PunchEventId,
                geoConfidenceScore = result.GeoConfidenceScore,
                geoOutcome = result.GeoOutcome.ToString(),
                fraudSignalCount = result.FraudSignals.Count
            })
            : Results.UnprocessableEntity(new
            {
                accepted = false,
                reason = result.RejectionReason,
                geoConfidenceScore = result.GeoConfidenceScore,
                geoOutcome = result.GeoOutcome.ToString(),
                fraudSignals = result.FraudSignals.Select(f => new { fraudType = f.Type.ToString(), severity = f.Severity.ToString(), f.Description })
            });
    }

    // ─── Geo Zones ───────────────────────────────────────────────────────────

    private static async Task<IResult> ListGeoZonesAsync(
        TimeAttendanceDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var zones = await db.GeoZones
            .Where(z => z.TenantId == tenantId)
            .OrderBy(z => z.Name)
            .Select(z => new
            {
                z.Id,
                z.Name,
                z.ZoneType,
                z.IsActive,
                z.CenterLatitude,
                z.CenterLongitude,
                z.RadiusMeters,
                z.GraceBufferMeters,
                z.ApproximateAreaSquareMeters
            })
            .ToListAsync(ct);

        return Results.Ok(zones);
    }

    private static async Task<IResult> GetGeoZoneAsync(
        Guid id,
        TimeAttendanceDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var zone = await db.GeoZones.FirstOrDefaultAsync(z => z.Id == id && z.TenantId == tenantId, ct);
        return zone is null ? Results.NotFound() : Results.Ok(zone);
    }

    private static async Task<IResult> CreateRadialZoneAsync(
        CreateRadialZoneRequest body,
        TimeAttendanceDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var zone = GeoZone.CreateRadial(
            tenantId, body.Name, body.Label,
            body.CenterLatitude, body.CenterLongitude,
            (int)body.RadiusMeters, body.Timezone,
            (int)body.GraceBufferMeters);

        db.GeoZones.Add(zone);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/attendance/geo-zones/{zone.Id}", new { zone.Id });
    }

    private static async Task<IResult> CreatePolygonZoneAsync(
        CreatePolygonZoneRequest body,
        TimeAttendanceDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var zone = GeoZone.CreatePolygon(
            tenantId, body.Name, body.Label, body.WktPolygon,
            body.ApproximateAreaSquareMeters, body.Timezone, (int)body.GraceBufferMeters);
        db.GeoZones.Add(zone);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/attendance/geo-zones/{zone.Id}", new { zone.Id });
    }

    private static async Task<IResult> ActivateZoneAsync(
        Guid id, TimeAttendanceDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var zone = await db.GeoZones.FirstOrDefaultAsync(z => z.Id == id && z.TenantId == tenantId, ct);
        if (zone is null) return Results.NotFound();
        zone.Activate();
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeactivateZoneAsync(
        Guid id, TimeAttendanceDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var zone = await db.GeoZones.FirstOrDefaultAsync(z => z.Id == id && z.TenantId == tenantId, ct);
        if (zone is null) return Results.NotFound();
        zone.Deactivate();
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    // ─── Biometric Enrollment ─────────────────────────────────────────────────

    private static async Task<IResult> StartEnrollmentSessionAsync(
        StartEnrollmentRequest body,
        TimeAttendanceDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var (tenantId, operatorEmployeeId) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var modality = Enum.Parse<BiometricModality>(body.Modality, ignoreCase: true);

        var session = BiometricEnrollmentSession.Start(
            tenantId,
            body.EmployeeId,
            modality,
            (operatorEmployeeId ?? Guid.Empty).ToString(),
            body.DeviceId,
            body.RequiredSampleCount);

        db.BiometricEnrollmentSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/attendance/enrollment/sessions/{session.Id}", new { session.Id, session.Status });
    }

    private static async Task<IResult> RecordConsentAsync(
        Guid id, TimeAttendanceDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var session = await db.BiometricEnrollmentSessions
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);
        if (session is null) return Results.NotFound();

        session.RecordConsent();
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { session.Status });
    }

    private static async Task<IResult> RecordSampleAsync(
        Guid id, [FromBody] RecordSampleRequest body,
        TimeAttendanceDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var session = await db.BiometricEnrollmentSessions
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);
        if (session is null) return Results.NotFound();

        var complete = session.RecordSample(body.QualityScore);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { session.Status, samplesComplete = complete });
    }

    private static async Task<IResult> CompleteEnrollmentAsync(
        Guid id, [FromBody] CompleteEnrollmentRequest body,
        TimeAttendanceDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var session = await db.BiometricEnrollmentSessions
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);
        if (session is null) return Results.NotFound();

        var template = BiometricTemplate.Enroll(
            tenantId,
            session.EmployeeId,
            session.Modality,
            body.EncryptedTemplateBlob,
            body.KmsKeyVersion,
            body.QualityScore,
            body.MatchMode,
            id);

        if (body.AuthorizedDeviceId.HasValue)
            template.AuthorizeDevice(body.AuthorizedDeviceId.Value);

        session.Complete(template.Id);
        db.BiometricTemplates.Add(template);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { session.Status, templateId = template.Id });
    }

    private static async Task<IResult> CancelEnrollmentAsync(
        Guid id, TimeAttendanceDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var session = await db.BiometricEnrollmentSessions
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);
        if (session is null) return Results.NotFound();
        session.Cancel();
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> GetEnrollmentSessionAsync(
        Guid id, TimeAttendanceDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var session = await db.BiometricEnrollmentSessions
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);
        return session is null ? Results.NotFound() : Results.Ok(session);
    }

    // ─── Biometric Templates ──────────────────────────────────────────────────

    private static async Task<IResult> GetEmployeeTemplatesAsync(
        Guid employeeId, TimeAttendanceDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var templates = await db.BiometricTemplates
            .Where(t => t.TenantId == tenantId && t.EmployeeId == employeeId)
            .Select(t => new { t.Id, t.Modality, t.IsActive, t.EnrolledAtUtc, t.RevokedAtUtc })
            .ToListAsync(ct);
        return Results.Ok(templates);
    }

    private static async Task<IResult> RevokeTemplateAsync(
        Guid id, TimeAttendanceDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var template = await db.BiometricTemplates
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId, ct);
        if (template is null) return Results.NotFound();
        template.Revoke();
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> AuthorizeDeviceAsync(
        Guid id, [FromBody] AuthorizeDeviceRequest body,
        TimeAttendanceDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var template = await db.BiometricTemplates
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId, ct);
        if (template is null) return Results.NotFound();
        template.AuthorizeDevice(body.DeviceId);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    // ─── Biometric Devices ────────────────────────────────────────────────────

    private static async Task<IResult> ListDevicesAsync(
        TimeAttendanceDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var devices = await db.BiometricDevices
            .Where(d => d.TenantId == tenantId)
            .Select(d => new { d.Id, d.SerialNumber, d.Vendor, d.Status, d.ZoneName, d.LastHeartbeatUtc })
            .ToListAsync(ct);
        return Results.Ok(devices);
    }

    private static async Task<IResult> RegisterDeviceAsync(
        RegisterDeviceRequest body, TimeAttendanceDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var device = BiometricDevice.Register(
            tenantId,
            body.SerialNumber, body.Vendor, body.Model,
            body.FirmwareVersion, body.CertificateThumbprint);

        db.BiometricDevices.Add(device);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/attendance/biometric-devices/{device.Id}", new { device.Id, device.Status });
    }

    private static async Task<IResult> ActivateDeviceAsync(
        Guid id, TimeAttendanceDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var device = await db.BiometricDevices.FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId, ct);
        if (device is null) return Results.NotFound();
        device.Activate();
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DecommissionDeviceAsync(
        Guid id, TimeAttendanceDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var device = await db.BiometricDevices.FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId, ct);
        if (device is null) return Results.NotFound();
        device.Decommission();
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> RecordHeartbeatAsync(
        Guid id, TimeAttendanceDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var device = await db.BiometricDevices.FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId, ct);
        if (device is null) return Results.NotFound();
        device.RecordHeartbeat();
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { device.Status, device.LastHeartbeatUtc });
    }

    // ─── Shift Definitions ────────────────────────────────────────────────────

    private static async Task<IResult> ListShiftDefinitionsAsync(
        TimeAttendanceDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var shifts = await db.ShiftDefinitions
            .Where(s => s.TenantId == tenantId)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Code,
                s.Category,
                s.IsActive,
                s.StartTime,
                s.EndTime,
                s.IsOvernight,
                s.IsNightShift,
                s.MinHoursPerDay,
                s.FlexWindowStart,
                s.FlexWindowEnd,
                s.LateArrivalGraceMinutes,
                s.EarlyDepartureGraceMinutes,
                s.AutoBreakDeductionMinutes,
                s.NightDifferentialMultiplier
            })
            .ToListAsync(ct);
        return Results.Ok(shifts);
    }

    private static async Task<IResult> CreateFixedShiftAsync(
        CreateFixedShiftRequest body, TimeAttendanceDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var shift = ShiftDefinition.CreateFixed(
            tenantId, body.Name, body.Code, body.StartTime, body.EndTime,
            body.LateGraceMinutes, body.EarlyGraceMinutes,
            body.AutoBreakDeductionMinutes, body.AutoBreakTriggerMinutes,
            body.NightDifferentialMultiplier);

        db.ShiftDefinitions.Add(shift);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/attendance/shift-definitions/{shift.Id}", new { shift.Id });
    }

    private static async Task<IResult> CreateFlexibleShiftAsync(
        CreateFlexibleShiftRequest body, TimeAttendanceDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var shift = ShiftDefinition.CreateFlexible(
            tenantId, body.Name, body.Code,
            body.WindowStart, body.WindowEnd, body.MinHoursPerDay,
            body.AutoBreakDeductionMinutes, body.AutoBreakTriggerMinutes);

        db.ShiftDefinitions.Add(shift);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/attendance/shift-definitions/{shift.Id}", new { shift.Id });
    }

    private static async Task<IResult> CreateRotatingShiftAsync(
        CreateRotatingShiftRequest body, TimeAttendanceDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var shift = ShiftDefinition.CreateRotating(
            tenantId, body.Name, body.Code, body.StartTime, body.EndTime,
            body.RotationCycleDays, body.LateGraceMinutes, body.EarlyGraceMinutes,
            body.NightDifferentialMultiplier);

        db.ShiftDefinitions.Add(shift);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/attendance/shift-definitions/{shift.Id}", new { shift.Id });
    }

    private static async Task<IResult> CreateSplitShiftAsync(
        CreateSplitShiftRequest body, TimeAttendanceDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var shift = ShiftDefinition.CreateSplit(
            tenantId, body.Name, body.Code,
            body.FirstStart, body.FirstEnd,
            body.SecondStart, body.SecondEnd,
            body.LateGraceMinutes, body.EarlyGraceMinutes);

        db.ShiftDefinitions.Add(shift);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/attendance/shift-definitions/{shift.Id}", new { shift.Id });
    }

    private static async Task<IResult> DeactivateShiftDefinitionAsync(
        Guid id, TimeAttendanceDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var shift = await db.ShiftDefinitions.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);
        if (shift is null) return Results.NotFound();
        shift.Deactivate();
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    // ─── Period Finalization ──────────────────────────────────────────────────

    private static async Task<IResult> FinalizeAttendancePeriodAsync(
        int year, int month,
        AttendancePeriodFinalizationService finalizationService,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        if (month < 1 || month > 12) return Results.BadRequest("Month must be 1-12.");
        if (year < 2000 || year > 2100) return Results.BadRequest("Invalid year.");

        var result = await finalizationService.FinalizeAsync(tenantId, year, month, ct);
        return Results.Ok(new
        {
            correlationId = result.CorrelationId,
            employeeCount = result.EmployeeCount,
            recordsFinalized = result.RecordsFinalized
        });
    }

    // ─── Request DTOs ─────────────────────────────────────────────────────────

    public sealed record PunchIngestionRequest(
        string Direction,
        string CaptureMode,
        Guid? DeviceId,
        DateTimeOffset? DeviceTimestampUtc,
        string? Modality,
        double? Latitude,
        double? Longitude,
        double? GpsAccuracyMeters,
        bool WifiSsidMatched,
        bool IpWhitelisted,
        bool BleBeaconDetected,
        bool VpnMatched,
        bool MockGpsDetected,
        bool JailbreakDetected,
        string? DeviceFingerprint,
        double? BiometricMatchScore,
        string? IdempotencyKey);

    public sealed record CreateRadialZoneRequest(
        string Name,
        string Label,
        double CenterLatitude,
        double CenterLongitude,
        double RadiusMeters,
        double GraceBufferMeters = 25,
        string Timezone = "UTC");

    public sealed record CreatePolygonZoneRequest(
        string Name,
        string Label,
        string WktPolygon,
        double ApproximateAreaSquareMeters,
        double GraceBufferMeters = 25,
        string Timezone = "UTC");

    public sealed record StartEnrollmentRequest(
        Guid EmployeeId,
        string Modality,
        Guid DeviceId,
        int RequiredSampleCount = 3);

    public sealed record RecordSampleRequest(int QualityScore);

    public sealed record CompleteEnrollmentRequest(
        string EncryptedTemplateBlob,
        string KmsKeyVersion,
        int QualityScore,
        BiometricMatchMode MatchMode = BiometricMatchMode.OnServer,
        Guid? AuthorizedDeviceId = null);

    public sealed record RegisterDeviceRequest(
        string SerialNumber,
        string Vendor,
        string Model,
        string FirmwareVersion,
        string CertificateThumbprint);

    public sealed record AuthorizeDeviceRequest(Guid DeviceId);

    public sealed record CreateFixedShiftRequest(
        string Name, string Code,
        TimeOnly StartTime, TimeOnly EndTime,
        int LateGraceMinutes = 15, int EarlyGraceMinutes = 15,
        int AutoBreakDeductionMinutes = 0, int AutoBreakTriggerMinutes = 360,
        decimal NightDifferentialMultiplier = 1.0m);

    public sealed record CreateFlexibleShiftRequest(
        string Name, string Code,
        TimeOnly WindowStart, TimeOnly WindowEnd,
        int MinHoursPerDay = 8,
        int AutoBreakDeductionMinutes = 0, int AutoBreakTriggerMinutes = 360);

    public sealed record CreateRotatingShiftRequest(
        string Name, string Code,
        TimeOnly StartTime, TimeOnly EndTime,
        int RotationCycleDays = 14,
        int LateGraceMinutes = 10, int EarlyGraceMinutes = 10,
        decimal NightDifferentialMultiplier = 1.0m);

    public sealed record CreateSplitShiftRequest(
        string Name, string Code,
        TimeOnly FirstStart, TimeOnly FirstEnd,
        TimeOnly SecondStart, TimeOnly SecondEnd,
        int LateGraceMinutes = 10, int EarlyGraceMinutes = 10);
}
