using Karamchari.Api.BFF.Attendance;
using Karamchari.Api.BFF.Billing;
using Karamchari.Api.BFF.Compliance;
using Karamchari.Api.BFF.Employee;
using Karamchari.Api.BFF.ESS;
using Karamchari.Api.BFF.Executive;
using Karamchari.Api.BFF.HR;
using Karamchari.Api.BFF.Manager;
using Karamchari.Api.BFF.Notifications;
using Karamchari.Api.BFF.Payroll;
using Karamchari.Api.BFF.PSA;
using Karamchari.Api.BFF.Search;
using Karamchari.Api.BFF.Tenants;
using Karamchari.Identity;
using Karamchari.PSA.Hubs;
using Karamchari.TimeAttendance.Hubs;
using Karamchari.Notifications.RealTime;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.DependencyInjection;

public static class EndpointExtensions
{
    public static WebApplication MapKaramchariEndpoints(this WebApplication app)
    {
        // Infrastructure
        app.MapIdentityEndpoints();

        // Hubs
        app.MapHub<AttendanceHub>("/hubs/attendance");
        app.MapHub<AnalyticsHub>("/hubs/analytics");
        app.MapHub<NotificationHub>("/hubs/notifications");

        // Analytics Daily Metrics (Remaining from Program.cs)
        app.MapGet("/api/analytics/projects/daily", GetDailyProjectMetrics);

        // Feature Modules
        app.MapTenantEndpoints();
        app.MapAttendanceEndpoints();
        app.MapPayrollEndpoints();
        app.MapComplianceEndpoints();
        app.MapPSAEndpoints();
        app.MapBillingEndpoints();
        app.MapESSEndpoints();

        // BFF Workspaces (Phase 1A)
        app.MapManagerWorkspace();
        app.MapEmployeeWorkspace();
        app.MapHRWorkspace();
        app.MapExecutiveWorkspace();
        app.MapNotificationCenter();
        app.MapExportJobs();
        app.MapSearch();

        // Payroll Specialized Endpoints (Phase 1A)
        app.MapFnFEndpoints();
        app.MapArrearEndpoints();
        app.MapCorrectionEndpoints();
        app.MapReimbursementEndpoints();
        app.MapLoanEndpoints();
        app.MapVariablePayEndpoints();
        app.MapSalaryRevisionEndpoints();
        app.MapSimulationEndpoints();
        app.MapDisbursementEndpoints();
        app.MapPayrollCockpitEndpoints();

        return app;
    }

    private static async Task<IResult> GetDailyProjectMetrics(
        [FromQuery] Guid? projectId,
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        TimeAttendanceDbContext db)
    {
        var start = string.IsNullOrEmpty(startDate) 
            ? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)) 
            : DateOnly.Parse(startDate);
            
        var end = string.IsNullOrEmpty(endDate) 
            ? DateOnly.FromDateTime(DateTime.UtcNow) 
            : DateOnly.Parse(endDate);

        var query = db.ProjectMetrics
            .Where(m => m.Date >= start && m.Date <= end);

        if (projectId.HasValue)
        {
            query = query.Where(m => m.ProjectId == projectId.Value);
        }

        var metrics = await query
            .OrderByDescending(m => m.Date)
            .ToListAsync();

        var lastUpdated = metrics.Max(m => (DateTimeOffset?)m.LastUpdatedAt) ?? DateTimeOffset.UtcNow;

        return Results.Ok(new { Data = metrics, LastUpdatedAt = lastUpdated });
    }
}
