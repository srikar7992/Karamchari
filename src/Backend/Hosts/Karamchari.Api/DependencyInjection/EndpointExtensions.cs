// -----------------------------------------------------------------------
// <copyright file="EndpointExtensions.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Api.BFF.Analytics;
using Karamchari.Api.BFF.AssetManagement;
using Karamchari.Api.BFF.Extensibility;
using Karamchari.Api.BFF.Forecasting;
using Karamchari.Api.BFF.Benefits;
using Karamchari.Api.BFF.Attendance;
using Karamchari.Api.BFF.Audit;
using Karamchari.Api.BFF.Billing;
using Karamchari.Api.BFF.Capability;
using Karamchari.Api.BFF.Common;
using Karamchari.Api.BFF.Compensation;
using Karamchari.Api.BFF.Compliance;
using Karamchari.Api.BFF.Employee;
using Karamchari.Api.BFF.Engagement;
using Karamchari.Api.BFF.ESS;
using Karamchari.Api.BFF.Executive;
using Karamchari.Api.BFF.Helpdesk;
using Karamchari.Api.BFF.HR;
using Karamchari.Api.BFF.Integration;
using Karamchari.Api.BFF.Intelligence;
using Karamchari.Api.BFF.Manager;
using Karamchari.Api.BFF.Notifications;
using Karamchari.Api.BFF.Onboarding;
using Karamchari.Api.BFF.Ops;
using Karamchari.Api.BFF.Payroll;
using Karamchari.Api.BFF.PlatformIntelligence;
using Karamchari.Api.BFF.PSA;
using Karamchari.Api.BFF.Search;
using Karamchari.Api.BFF.Succession;
using Karamchari.Api.BFF.Tenants;
using Karamchari.Api.BFF.Workflow;
using Karamchari.Api.EndpointRegistry;
using Karamchari.Identity;
using Karamchari.Notifications.RealTime;
using Karamchari.PSA.Hubs;
using Karamchari.TimeAttendance.Hubs;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.DependencyInjection;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public static class EndpointExtensions
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static WebApplication MapKaramchariEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Infrastructure
        app.MapIdentityEndpoints();

        // Hubs. Authenticated: SignalR clients pass the bearer token via the access_token
        // query parameter, which the JWT handler maps for /hubs paths (see
        // ConfigureJwtBearerOptions.OnMessageReceived in Karamchari.Identity.Infrastructure).
        app.MapHub<AttendanceHub>("/hubs/attendance").RequireAuthorization();
        app.MapHub<AnalyticsHub>("/hubs/analytics").RequireAuthorization();
        app.MapHub<NotificationHub>("/hubs/notifications").RequireAuthorization();

        // Analytics Daily Metrics (Remaining from Program.cs)
        app.MapGet("/api/analytics/projects/daily", GetDailyProjectMetrics)
            .RequireAuthorization();

        // Operational endpoint registry (route/module/authorization audit surface)
        app.MapEndpointCatalogEndpoints();

        // Map Endpoints via Capability Modules
        var modules = CapabilityRegistry.GetModules(app.Configuration);
        foreach (var module in modules)
        {
            module.MapEndpoints(app);
        }

        // Feature Modules (BFF/Experience Layer)
        app.MapTenantEndpoints();
        app.MapApprovalEndpoints();
        app.MapAttendanceEndpoints();
        app.MapBiometricAttendanceEndpoints();
        app.MapAttendanceReportingEndpoints();
        app.MapLeaveEndpoints();
        app.MapRosterEndpoints();
        app.MapCapabilityEndpoints();
        app.MapAuditEndpoints();
        app.MapAnalyticsEndpoints();
        app.MapWorkforcePlanningEndpoints();
        app.MapCompensationEndpoints();
        app.MapSuccessionEndpoints();
        app.MapExtensibilityEndpoints();
        app.MapStrategyEndpoints();
        app.MapWorkforceIntelligenceEndpoints();
        app.MapInterventionEndpoints();
        app.MapPayrollEndpoints();
        app.MapComplianceEndpoints();
        app.MapGovernanceEndpoints();
        app.MapPlatformIntelligenceEndpoints();
        app.MapPSAEndpoints();
        app.MapBillingEndpoints();
        app.MapESSEndpoints();
        app.MapEmployeeEndpoints();

        // New modules: Helpdesk, Asset Management, Engagement, Succession
        app.MapHelpdeskEndpoints();
        app.MapAssetManagementEndpoints();
        app.MapEngagementEndpoints();
        app.MapSuccessionEndpoints();
        app.MapIntegrationEndpoints();
        app.MapOnboardingEndpoints();
        app.MapCompensationEndpoints();
        app.MapBenefitsEndpoints();
        app.MapAuditEndpoints();
        app.MapComplianceGateEndpoints();
        app.MapWorkflowRuleEndpoints();
        app.MapWorkflowApprovalEndpoints();
        app.MapOpsEndpoints();
        app.MapCatalogEndpoints();

        // BFF Workspaces (Phase 1A)
        app.MapManagerWorkspace();
        app.MapEmployeeWorkspace();
        app.MapHRWorkspace();
        app.MapExecutiveWorkspace();
        app.MapOperationsDashboardEndpoints();
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
