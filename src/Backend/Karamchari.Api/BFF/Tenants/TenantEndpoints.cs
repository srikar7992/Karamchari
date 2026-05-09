using Karamchari.Api.BFF.Common;
using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Core.Persistence.Provisioning;
using MassTransit;

namespace Karamchari.Api.BFF.Tenants;

public static class TenantEndpoints
{
    public static WebApplication MapTenantEndpoints(this WebApplication app)
    {
        app.MapPost("/api/tenants", ProvisionTenant);
        return app;
    }

    private static async Task<IResult> ProvisionTenant(
        ProvisionTenantRequest request, 
        TenantProvisioningService provisioningService,
        IPublishEndpoint publishEndpoint)
    {
        var tenantId = request.CompanyName.ToLowerInvariant().Replace(" ", "_");
        var schemaName = $"tenant_{tenantId}";
        
        await provisioningService.ProvisionTenantAsync(tenantId, schemaName);
        
        await publishEndpoint.Publish(new TenantProvisionedIntegrationEvent(
            tenantId, 
            request.CompanyName, 
            request.AdminEmail));
        
        return Results.Created($"/api/tenants/{tenantId}", new { TenantId = tenantId, SchemaName = schemaName });
    }
}
