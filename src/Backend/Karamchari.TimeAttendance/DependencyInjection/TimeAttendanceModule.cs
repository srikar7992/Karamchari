using Karamchari.Core.DependencyInjection;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.TimeAttendance.DependencyInjection;

/// <summary>
/// Capability pack registration for the TimeAttendance module.
/// </summary>
public sealed class TimeAttendanceModule : ICapabilityModule
{
    private readonly IConfiguration _configuration;

    public TimeAttendanceModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddKaramchariTimeAttendance(_configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        // Add TimeAttendance endpoints here
    }

    public void RegisterConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddKaramchariTimeAttendanceConsumers();
    }
}
