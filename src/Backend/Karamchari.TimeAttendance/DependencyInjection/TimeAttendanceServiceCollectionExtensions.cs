using Karamchari.Core.DependencyInjection;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.TimeAttendance.DependencyInjection;

/// <summary>
/// Extension methods for registering Time and Attendance module services.
/// Phase 1C: Workforce Operational Intelligence Platform.
/// </summary>
public static class TimeAttendanceServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    public static IServiceCollection AddKaramchariTimeAttendance(
        this IServiceCollection services,
        IConfiguration configuration,
        MassTransit.IBusRegistrationConfigurator busConfigurator)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(busConfigurator);

        busConfigurator.AddConsumer<Karamchari.TimeAttendance.Consumers.TenantProvisionedConsumer>();
        busConfigurator.AddConsumer<Karamchari.TimeAttendance.Consumers.TimesheetApprovedConsumer>();
        busConfigurator.AddConsumer<Karamchari.TimeAttendance.Consumers.TimesheetApprovedAnalyticsConsumer>();
        busConfigurator.AddConsumer<Karamchari.TimeAttendance.Consumers.BillingAnalyticsConsumer>();

        // ── RLS: Workforce operational intelligence (Phase 1C) ────────────────
        
        services.RegisterTenantTable("Workforce_ShiftDefinitions");
        services.RegisterTenantTable("Workforce_Schedules");
        services.RegisterTenantTable("Workforce_ShiftAssignments");
        services.RegisterTenantTable("Workforce_AttendanceSessions");
        services.RegisterTenantTable("Workforce_AttendanceEvents");
        services.RegisterTenantTable("Workforce_AttendanceAnomalies");
        services.RegisterTenantTable("Workforce_AttendancePolicies");
        services.RegisterTenantTable("Workforce_ComplianceRules");

        // ── Legacy Core Tables ───────────────────────────────────────────────
        
        services.RegisterTenantTable("HolidayCalendars");
        services.RegisterTenantTable("Holidays");
        services.RegisterTenantTable("LeavePolicies");
        services.RegisterTenantTable("LeaveRequests");
        services.RegisterTenantTable("LeaveBalances");
        services.RegisterTenantTable("Timesheets");

        // ── Analytics ────────────────────────────────────────────────────────
        
        services.RegisterTenantTable("Analytics_ProjectMetrics");
        services.RegisterTenantTable("Analytics_ProcessedEventLog");

        services.AddDbContext<TimeAttendanceDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"ConnectionStrings:{ConnectionStringName} must be configured before TimeAttendanceDbContext can be resolved.");

            options.UseSqlServer(connectionString);
            options.AddKaramchariInterceptors(serviceProvider);
        });

        services.AddScoped<LeaveRequestService>();
        services.AddScoped<Karamchari.TimeAttendance.Services.TimesheetService>();
        services.AddSingleton<Karamchari.TimeAttendance.Services.ICapacityProvider,
                              Karamchari.TimeAttendance.Services.NullCapacityProvider>();
        
        services.AddScoped<Karamchari.TimeAttendance.Services.ShiftRosteringEngine>();

        return services;
    }
}
