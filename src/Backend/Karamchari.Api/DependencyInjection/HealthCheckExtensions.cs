using Karamchari.Billing.Persistence;
using Karamchari.Compensation.Persistence;
using Karamchari.HR.Persistence;
using Karamchari.Notifications.Persistence;
using Karamchari.Payroll.Data;
using Karamchari.Performance.Persistence;
using Karamchari.PSA.Persistence;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Karamchari.Api.DependencyInjection;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static IServiceCollection AddKaramchariHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var healthBuilder = services.AddHealthChecks();

        // Database Checks
        healthBuilder.AddDbContextCheck<HRDbContext>("Database:HR");
        healthBuilder.AddDbContextCheck<PayrollDbContext>("Database:Payroll");
        healthBuilder.AddDbContextCheck<TimeAttendanceDbContext>("Database:TimeAttendance");
        healthBuilder.AddDbContextCheck<PSADbContext>("Database:PSA");
        healthBuilder.AddDbContextCheck<BillingDbContext>("Database:Billing");
        healthBuilder.AddDbContextCheck<PerformanceDbContext>("Database:Performance");
        healthBuilder.AddDbContextCheck<NotificationsDbContext>("Database:Notifications");
        healthBuilder.AddDbContextCheck<CompensationDbContext>("Database:Compensation");

        // Messaging Checks
        var sbConnection = configuration.GetConnectionString("AzureServiceBus");
        if (!string.IsNullOrEmpty(sbConnection))
        {
            healthBuilder.AddAzureServiceBusTopic(sbConnection, topicName: "integration-events", name: "ServiceBus:Events");
        }

        // External Dependencies (Mock examples)
        // healthBuilder.AddUrlGroup(new Uri("https://api.tin-nsdl.com/health"), "External:NSDL");

        return services;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static WebApplication MapKaramchariHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            },
            ResponseWriter = WriteResponse
        });

        // Readiness vs Liveness
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteResponse
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false, // Always returns 200 if the app is up
            ResponseWriter = WriteResponse
        });

        return app;
    }

    private static Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration,
            results = report.Entries.Select(e => new
            {
                key = e.Key,
                value = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration,
                data = e.Value.Data
            })
        });

        return context.Response.WriteAsync(json);
    }
}
