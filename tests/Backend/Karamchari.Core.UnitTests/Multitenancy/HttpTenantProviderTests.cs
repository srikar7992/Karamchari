// -----------------------------------------------------------------------
// <copyright file="HttpTenantProviderTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using FluentAssertions;
using Karamchari.Core.Multitenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Karamchari.Core.UnitTests.Multitenancy;

public sealed class HttpTenantProviderTests
{
    [Fact]
    public void GetTenant_WhenJwtClaimIsPresent_ReturnsJwtTenant()
    {
        var context = CreateContext();
        context.User = CreateUser(("tenant_id", "Acme"));

        var tenant = CreateProvider(context).GetTenant();

        tenant.TenantId.Should().Be("acme");
        tenant.SchemaName.Should().Be("tenant_acme");
        tenant.Source.Should().Be(TenantSource.JwtClaim);
    }

    [Fact]
    public void GetTenant_WhenJwtAndSubdomainDisagree_ThrowsSourcesDisagree()
    {
        var context = CreateContext(host: "globex.karamchari.com");
        context.User = CreateUser(("tenant_id", "acme"));

        var act = () => CreateProvider(context).GetTenant();

        act.Should()
            .Throw<TenantResolutionException>()
            .Where(ex => ex.Reason == TenantResolutionFailureReason.SourcesDisagree);
    }

    [Fact]
    public void GetTenant_WhenTrustedHeaderHasValidGatewayProof_ReturnsHeaderTenant()
    {
        var context = CreateContext();
        context.Request.Headers["X-Tenant-Id"] = "Globex";
        context.Request.Headers["X-Karamchari-Gateway"] = "gateway-proof";

        var tenant = CreateProvider(
            context,
            new TenantOptions
            {
                RequireSubdomainAgreement = false,
                TrustedGatewayFingerprint = "gateway-proof",
            }).GetTenant();

        tenant.TenantId.Should().Be("globex");
        tenant.Source.Should().Be(TenantSource.TrustedHeader);
    }

    [Fact]
    public void GetTenant_WhenTenantHeaderHasNoGatewayProof_ThrowsUntrustedHeaderSource()
    {
        var context = CreateContext();
        context.Request.Headers["X-Tenant-Id"] = "acme";

        var act = () => CreateProvider(
            context,
            new TenantOptions
            {
                RequireSubdomainAgreement = false,
                TrustedGatewayFingerprint = "gateway-proof",
            }).GetTenant();

        act.Should()
            .Throw<TenantResolutionException>()
            .Where(ex => ex.Reason == TenantResolutionFailureReason.UntrustedHeaderSource);
    }

    [Fact]
    public void GetTenant_WhenJwtTenantIdIsInvalid_ThrowsInvalidTenantIdFormat()
    {
        var context = CreateContext();
        context.User = CreateUser(("tenant_id", "bad tenant"));

        var act = () => CreateProvider(context).GetTenant();

        act.Should()
            .Throw<TenantResolutionException>()
            .Where(ex => ex.Reason == TenantResolutionFailureReason.InvalidTenantIdFormat);
    }

    [Fact]
    public void TryGetTenant_WhenTenantCannotBeResolved_ReturnsFalse()
    {
        var context = CreateContext();

        var resolved = CreateProvider(context).TryGetTenant(out var tenant);

        resolved.Should().BeFalse();
        tenant.Should().BeNull();
    }

    private static DefaultHttpContext CreateContext(string host = "acme.karamchari.com")
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString(host);
        return context;
    }

    private static ClaimsPrincipal CreateUser(params (string Type, string Value)[] claims) =>
        new(new ClaimsIdentity(
            claims.Select(claim => new Claim(claim.Type, claim.Value)),
            authenticationType: "Test"));

    private static HttpTenantProvider CreateProvider(
        HttpContext context,
        TenantOptions? options = null)
    {
        var accessor = new HttpContextAccessor { HttpContext = context };

        return new HttpTenantProvider(
            accessor,
            Options.Create(options ?? new TenantOptions()),
            NullLogger<HttpTenantProvider>.Instance);
    }
}
