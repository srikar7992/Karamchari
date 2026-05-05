using Karamchari.Core.DependencyInjection;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.TimeAttendance.DependencyInjection;

/// <summary>
/// Extension methods for registering Time and Attendance module services.
/// </summary>
public static class TimeAttendanceServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    /// <summary>
    /// Adds the Time and Attendance module services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="busConfigurator">The MassTransit bus registration configurator.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddKaramchariTimeAttendance(
        this IServiceCollection services,
        IConfiguration configuration,
        MassTransit.IBusRegistrationConfigurator busConfigurator)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(busConfigurator);

        busConfigurator.AddConsumer<Karamchari.TimeAttendance.Consumers.TenantProvisionedConsumer>();

        // RLS: Register tables for multi-tenancy enforcement
        services.RegisterTenantTable("HolidayCalendars");
        services.RegisterTenantTable("Holidays");
        services.RegisterTenantTable("LeavePolicies");
        services.RegisterTenantTable("LeaveRequests");
        services.RegisterTenantTable("LeaveBalances");
        services.RegisterTenantTable("Timesheets");

        // IoT & Edge Tables
        services.RegisterTenantTable("IoT_Devices");
        services.RegisterTenantTable("IoT_BiometricMappings");
        services.RegisterTenantTable("IoT_RawPunches");
        services.RegisterTenantTable("IoT_GeoFences");
        services.RegisterTenantTable("IoT_FraudFlags");
        services.RegisterTenantTable("IoT_LiveAttendance");
        services.RegisterTenantTable("IoT_AttendanceResults");
        services.RegisterTenantTable("IoT_AttendanceAudits");

        // Shift Tables
        services.RegisterTenantTable("Shifts_Templates");
        services.RegisterTenantTable("Shifts_Assignments");
        services.RegisterTenantTable("Shifts_Overrides");
        services.RegisterTenantTable("Shifts_WeeklyOffRules");

        services.AddDbContext<TimeAttendanceDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"ConnectionStrings:{ConnectionStringName} must be configured before TimeAttendanceDbContext can be resolved.");

            options.UseSqlServer(connectionString);
            options.AddKaramchariInterceptors(serviceProvider);
        });

        services.AddScoped<LeaveRequestService>();
        
        // Edge Services
        services.AddScoped<Karamchari.TimeAttendance.Edge.DeviceAuthService>();
        services.AddScoped<Karamchari.TimeAttendance.Edge.IngestionPublisher>();

        // Reconciliation & Replay Services
        services.AddScoped<Karamchari.TimeAttendance.Services.ShiftRosteringEngine>();
        services.AddScoped<Karamchari.TimeAttendance.Services.ShiftReconciliationEngine>();
        services.AddScoped<Karamchari.TimeAttendance.Services.ReprocessingWorker>();
        
        // Live Attendance & Fraud Services
        services.AddScoped<Karamchari.TimeAttendance.Services.LiveAttendanceService>();
        services.AddScoped<Karamchari.TimeAttendance.Services.AsyncFraudDetectionWorker>();

        return services;
    }
}
