using Karamchari.Api.BFF.Common;
using Karamchari.TimeAttendance.Edge;
using Karamchari.TimeAttendance.Persistence;
using Karamchari.TimeAttendance.Services.Fraud;
using Karamchari.TimeAttendance.Services.Geofencing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.IoT;

public static class IoTEndpoints
{
    public static WebApplication MapIoTEndpoints(this WebApplication app)
    {
        var iot = app.MapGroup("/api/iot").RequireAuthorization();

        iot.MapPost("/push", PushPunch);
        iot.MapPost("/mobile-push", PushMobilePunch);

        var mobile = app.MapGroup("/api/mobile").RequireAuthorization();
        mobile.MapPost("/punch", MobileAppPunch);

        return app;
    }

    private static async Task<IResult> PushPunch(
        HttpRequest request,
        DeviceAuthService authService,
        IngestionPublisher publisher)
    {
        var apiKey = request.Headers["x-api-key"].ToString();
        var authResult = await authService.ValidateAsync(apiKey);

        if (!authResult.IsValid || authResult.Device == null)
            return Results.Unauthorized();

        using var reader = new StreamReader(request.Body);
        var rawPayload = await reader.ReadToEndAsync();
        await publisher.PublishAsync(rawPayload, authResult.Device.DeviceId, authResult.Device.TenantId, isMobile: false);
        return Results.Ok();
    }

    private static async Task<IResult> PushMobilePunch(
        HttpRequest request,
        DeviceAuthService authService,
        IngestionPublisher publisher)
    {
        var apiKey = request.Headers["x-api-key"].ToString();
        var authResult = await authService.ValidateAsync(apiKey);

        if (!authResult.IsValid || authResult.Device == null)
            return Results.Unauthorized();

        using var reader = new StreamReader(request.Body);
        var rawPayload = await reader.ReadToEndAsync();
        await publisher.PublishAsync(rawPayload, authResult.Device.DeviceId, authResult.Device.TenantId, isMobile: true);
        return Results.Ok();
    }

    private static async Task<IResult> MobileAppPunch(
        MobilePunchRequest request,
        TimeAttendanceDbContext dbContext,
        IGeofenceService geofenceService,
        IFraudRiskEngine fraudEngine,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("MobilePunch");
        var existing = await dbContext.RawPunches.AnyAsync(p => p.ExternalId == request.ClientId);
        if (existing) return Results.Ok(new { message = "Punch already processed (idempotent)" });

        if (!Guid.TryParse(request.EmployeeId, out var employeeId)) return Results.BadRequest("Invalid Employee ID format");

        var timeSkew = Math.Abs((DateTime.UtcNow - request.Timestamp.ToUniversalTime()).TotalMinutes);
        if (timeSkew > 1440) logger.LogWarning("Significant time skew detected for {EmployeeId}: {Skew} minutes", employeeId, timeSkew);

        var (isInside, boundary, distance) = await geofenceService.CheckGeofenceAsync(employeeId, request.Lat, request.Lon);
        if (!isInside)
        {
            logger.LogWarning("Punch rejected: Outside geofence for {EmployeeId} (Distance: {Distance:F0}m)", request.EmployeeId, distance);
            return Results.BadRequest(new { 
                message = "Outside geofence boundary", 
                distanceMeters = Math.Round(distance),
                requiredBoundary = boundary?.Name ?? "Assigned Site"
            });
        }

        var punch = Karamchari.TimeAttendance.Domain.IoT.RawPunch.Create(
            "system", employeeId, "MOBILE_APP", request.ClientId, request.Timestamp.ToUniversalTime(), request.Timestamp, "UTC", DateTime.UtcNow, request.Type, Karamchari.TimeAttendance.Domain.IoT.PunchSource.Mobile, request.Lat, request.Lon);

        dbContext.RawPunches.Add(punch);
        await dbContext.SaveChangesAsync();

        var fraudFlag = await fraudEngine.EvaluatePunchAsync(punch, request.Accuracy);
        return Results.Ok(new { 
            message = "Punch recorded successfully", 
            id = punch.Id,
            riskLevel = fraudFlag.Severity,
            explanation = fraudFlag.Explanation
        });
    }
}
