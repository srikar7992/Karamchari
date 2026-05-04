using Karamchari.Core.DependencyInjection;
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
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddKaramchariTimeAttendance(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // RLS: Register tables for multi-tenancy enforcement
        services.RegisterTenantTable("HolidayCalendars");
        services.RegisterTenantTable("Holidays");
        services.RegisterTenantTable("LeavePolicies");
        services.RegisterTenantTable("LeaveRequests");
        services.RegisterTenantTable("LeaveBalances");

        services.AddDbContext<TimeAttendanceDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"ConnectionStrings:{ConnectionStringName} must be configured before TimeAttendanceDbContext can be resolved.");

            options.UseSqlServer(connectionString);
            options.AddKaramchariInterceptors(serviceProvider);
        });

        return services;
    }
}
