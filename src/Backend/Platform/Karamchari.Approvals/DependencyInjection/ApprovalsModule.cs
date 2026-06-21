using Karamchari.Approvals.Consumers;
using Karamchari.Approvals.Persistence;
using Karamchari.Core.DependencyInjection;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Approvals.DependencyInjection;

public sealed class ApprovalsModule(IConfiguration configuration) : ICapabilityModule
{
    public void RegisterServices(IServiceCollection services)
    {
        services.AddKaramchariApprovals(configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        // ESS approval action endpoints are mapped by ESSEndpoints in the API host.
        // Approvals module itself has no standalone endpoints — it is consumed by ESS.
    }

    public void RegisterConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddEntityFrameworkOutbox<ApprovalsDbContext>(o =>
        {
            o.UseBusOutbox();
        });
        configurator.AddConsumer<ApprovalActionedNotificationConsumer>();
    }
}
