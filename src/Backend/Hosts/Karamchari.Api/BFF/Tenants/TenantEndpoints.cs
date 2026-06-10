// -----------------------------------------------------------------------
// <copyright file="TenantEndpoints.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Api.BFF.Common;
using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Core.Persistence.Provisioning;
using MassTransit;

namespace Karamchari.Api.BFF.Tenants;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public static class TenantEndpoints
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static WebApplication MapTenantEndpoints(this WebApplication app)
    {
        // Tenant provisioning creates schemas and publishes provisioning events — it must
        // never be anonymous. (Bootstrap provisioning uses TenantProvisioningService directly,
        // not this endpoint, so requiring auth here does not affect first-run setup.)
        app.MapPost("/api/tenants", ProvisionTenant).RequireAuthorization();
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
