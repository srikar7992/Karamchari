using Karamchari.Core.DependencyInjection;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Recruitment.DependencyInjection;

/// <summary>
/// Capability pack registration for the Recruitment module.
/// </summary>
public sealed class RecruitmentModule : ICapabilityModule
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public RecruitmentModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public void RegisterServices(IServiceCollection services)
    {
        services.AddKaramchariRecruitment(_configuration);
    }

    /// <inheritdoc/>
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
    }

    /// <inheritdoc/>
    public void RegisterConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddKaramchariRecruitmentConsumers();
    }
}
