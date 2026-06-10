// -----------------------------------------------------------------------
// <copyright file="EndpointCatalogTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Karamchari.Api.EndpointRegistry;
using Xunit;

namespace Karamchari.SecurityTests;

/// <summary>
/// Endpoint catalog audit: every mapped endpoint must appear in the catalog and must
/// declare its authorization posture explicitly — either authorization metadata
/// (RequireAuthorization / [Authorize]) or an explicit AllowAnonymous. Endpoints that
/// are anonymous merely because nobody added metadata are treated as defects.
/// </summary>
public sealed class EndpointCatalogTests : IClassFixture<SecurityWebApplicationFactory>
{
    /// <summary>
    /// Route prefixes excluded from the explicit-authorization rule. Each exclusion is a
    /// known framework or transport surface, not a business endpoint. Shrink this list;
    /// never grow it without a documented reason.
    /// </summary>
    private static readonly string[] ExemptRoutePrefixes =
    [
        // Liveness/readiness probes must be reachable by orchestrators without credentials.
        "/health",

        // OpenAPI document and Scalar UI are mapped in Development only.
        "/openapi",
        "/scalar",
    ];

    private readonly SecurityWebApplicationFactory _factory;

    public EndpointCatalogTests(SecurityWebApplicationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    [Fact]
    public async Task EndpointCatalog_RequiresAuthentication()
    {
        var client = _factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync("/api/v1/ops/endpoint-catalog");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EndpointCatalog_ListsEveryMappedEndpoint_WithRouteAndModule()
    {
        var entries = await GetCatalogAsync();

        entries.Should().NotBeEmpty();
        entries.Should().OnlyContain(entry => !string.IsNullOrWhiteSpace(entry.Route));
        entries.Should().OnlyContain(entry => !string.IsNullOrWhiteSpace(entry.Module));

        // The catalog endpoint documents itself.
        entries.Should().Contain(entry => entry.Route == "/api/v1/ops/endpoint-catalog");
    }

    [Fact]
    public async Task EveryEndpoint_DeclaresAuthorizationPosture()
    {
        var entries = await GetCatalogAsync();

        var violations = entries
            .Where(entry => !entry.RequiresAuthorization && !entry.AllowsAnonymous)
            .Where(entry => !ExemptRoutePrefixes.Any(prefix =>
                entry.Route.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .Select(entry => $"{string.Join('/', entry.HttpMethods)} {entry.Route} ({entry.Module})")
            .ToList();

        violations.Should().BeEmpty(
            "every endpoint must call RequireAuthorization() or AllowAnonymous() explicitly; " +
            "implicit anonymous endpoints are security defects");
    }

    [Fact]
    public async Task AnonymousEndpoints_AreTheKnownCredentialAcquisitionSet()
    {
        var entries = await GetCatalogAsync();

        var anonymousRoutes = entries
            .Where(entry => entry.AllowsAnonymous)
            .Where(entry => entry.Route != "/" && !ExemptRoutePrefixes.Any(prefix =>
                entry.Route.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .Select(entry => entry.Route)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        anonymousRoutes.Should().BeEquivalentTo(
            new[]
            {
                // Credential acquisition cannot require credentials; rate-limited per IP.
                "/api/identity/login",
                "/api/identity/refresh",
                "/api/identity/register",

                // Event catalog reads are anonymous by design: static event model metadata,
                // no tenant data (see BFF/Ops/CatalogEndpoints.cs).
                "/api/v1/catalog/events/",
                "/api/v1/catalog/events/{eventName}",
                "/api/v1/catalog/events/consumer/{consumer}",
                "/api/v1/catalog/events/publisher/{publisher}",
            },
            "new anonymous endpoints must be reviewed and added here deliberately");
    }

    private async Task<IReadOnlyList<EndpointCatalogEntry>> GetCatalogAsync()
    {
        var client = _factory.CreateAuthenticatedClient("acme", role: "Admin");

        var entries = await client.GetFromJsonAsync<List<EndpointCatalogEntry>>(
            "/api/v1/ops/endpoint-catalog");

        entries.Should().NotBeNull();
        return entries!;
    }
}
