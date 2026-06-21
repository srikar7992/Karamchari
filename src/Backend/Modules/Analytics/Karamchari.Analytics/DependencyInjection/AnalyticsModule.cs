using Karamchari.Analytics.Consumers;
using Karamchari.Analytics.Persistence;
using Karamchari.Analytics.Services;
using Karamchari.Core.DependencyInjection;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Analytics.DependencyInjection;

public sealed class AnalyticsModule(IConfiguration configuration) : ICapabilityModule
{
    private const string ConnStr = "KaramchariDb";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddDbContext<AnalyticsDbContext>((_, opts) =>
        {
            var cs = configuration.GetConnectionString(ConnStr)
                ?? throw new InvalidOperationException($"ConnectionStrings:{ConnStr} not configured.");
            opts.UseSqlServer(cs, sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null));
        });
        services.AddScoped<AnalyticsQueryService>();
        services.AddScoped<WorkforceScorecardService>();
        services.AddScoped<AnalyticsReconciliationService>();
        services.AddScoped<ProjectionLagHealthCheck>();
        services.AddHostedService<WorkforceSnapshotJob>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app) { }

    public void RegisterConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<EmployeeHiredAnalyticsConsumer>();
        configurator.AddConsumer<EmployeeTerminatedAnalyticsConsumer>();
        configurator.AddConsumer<PayrollRunCompletedAnalyticsConsumer>();
        configurator.AddEntityFrameworkOutbox<AnalyticsDbContext>(o => o.UseBusOutbox());
    }
}
