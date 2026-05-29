using Karamchari.Billing.Persistence;
using Karamchari.Capability.Persistence;
using Karamchari.Compensation.Persistence;
using Karamchari.Core.Persistence;
using Karamchari.Forecasting.Persistence;
using Karamchari.Governance.Persistence;
using Karamchari.HR.Persistence;
using Karamchari.Intelligence.Persistence;
using Karamchari.Notifications.Persistence;
using Karamchari.Payroll.Data;
using Karamchari.Performance.Persistence;
using Karamchari.PSA.Persistence;
using Karamchari.Recruitment.Persistence;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Karamchari.Api.DependencyInjection;

/// <summary>
/// Centralized health check registration for the platform.
/// </summary>
public static class HealthCheckExtensions
{
    private static readonly string[] ReadyTags = new[] { "ready" };
    private static readonly string[] DbTags = new[] { "ready", "db" };
    private static readonly string[] MessagingTags = new[] { "ready", "messaging" };
    private static readonly string[] CacheTags = new[] { "ready", "cache" };

    /// <summary>
    /// Registers all platform health checks including databases, messaging, and caching.
    /// </summary>
    public static IServiceCollection AddKaramchariHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var healthBuilder = services.AddHealthChecks();

        // 1. Database Checks (Bounded Contexts)
        healthBuilder.AddDbContextCheck<CoreDbContext>("Database:Core", tags: DbTags);
        healthBuilder.AddDbContextCheck<HRDbContext>("Database:HR", tags: DbTags);
        healthBuilder.AddDbContextCheck<PayrollDbContext>("Database:Payroll", tags: DbTags);
        healthBuilder.AddDbContextCheck<TimeAttendanceDbContext>("Database:TimeAttendance", tags: DbTags);
        healthBuilder.AddDbContextCheck<PSADbContext>("Database:PSA", tags: DbTags);
        healthBuilder.AddDbContextCheck<BillingDbContext>("Database:Billing", tags: DbTags);
        healthBuilder.AddDbContextCheck<PerformanceDbContext>("Database:Performance", tags: DbTags);
        healthBuilder.AddDbContextCheck<NotificationsDbContext>("Database:Notifications", tags: DbTags);
        healthBuilder.AddDbContextCheck<CompensationDbContext>("Database:Compensation", tags: DbTags);
        healthBuilder.AddDbContextCheck<RecruitmentDbContext>("Database:Recruitment", tags: DbTags);
        healthBuilder.AddDbContextCheck<CapabilityDbContext>("Database:Capability", tags: DbTags);
        healthBuilder.AddDbContextCheck<IntelligenceDbContext>("Database:Intelligence", tags: DbTags);
        healthBuilder.AddDbContextCheck<GovernanceDbContext>("Database:Governance", tags: DbTags);
        healthBuilder.AddDbContextCheck<ForecastingDbContext>("Database:Forecasting", tags: DbTags);

        // 2. Messaging Checks
        var rabbitMqConnection = configuration.GetConnectionString("RabbitMQ");
        if (!string.IsNullOrEmpty(rabbitMqConnection))
        {
            healthBuilder.AddRabbitMQ(
                rabbitMqConnection,
                name: "Messaging:RabbitMQ",
                tags: MessagingTags);
        }

        var sbConnection = configuration.GetConnectionString("AzureServiceBus");
        if (!string.IsNullOrEmpty(sbConnection))
        {
            healthBuilder.AddAzureServiceBusTopic(sbConnection, topicName: "integration-events", name: "Messaging:ServiceBus", tags: MessagingTags);
        }

        // 3. Caching Checks
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            healthBuilder.AddRedis(redisConnectionString: redisConnection, name: "Caching:Redis", tags: CacheTags);
        }

        return services;
    }

    /// <summary>
    /// Maps health check endpoints to the request pipeline.
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

        // Startup probe: dependencies must be reachable before the pod begins serving.
        // Uses the readiness checks; Kubernetes stops probing once it first succeeds.
        app.MapHealthChecks("/health/startup", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            },
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
