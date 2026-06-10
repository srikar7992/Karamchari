// -----------------------------------------------------------------------
// <copyright file="EndpointCatalogEndpoints.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Api.EndpointRegistry;

/// <summary>
/// Exposes the endpoint catalog over HTTP for operational verification
/// (health scripts, smoke tests, route audits).
/// </summary>
public static class EndpointCatalogEndpoints
{
    /// <summary>
    /// Maps <c>GET /api/v1/ops/endpoint-catalog</c>. The catalog is rebuilt per request
    /// from the live endpoint data sources, so it always reflects what is actually mapped.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The same application, for chaining.</returns>
    public static WebApplication MapEndpointCatalogEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Capture the live data source collection; by request time it contains every
        // endpoint mapped during startup, including ones mapped after this call.
        var dataSources = ((IEndpointRouteBuilder)app).DataSources;

        app.MapGet(
                "/api/v1/ops/endpoint-catalog",
                () => Results.Ok(EndpointCatalogBuilder.Build(dataSources)))
            .WithName("Ops.EndpointCatalog")
            .RequireAuthorization();

        return app;
    }
}
