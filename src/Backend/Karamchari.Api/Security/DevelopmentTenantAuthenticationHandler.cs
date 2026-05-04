using System.Security.Claims;
using System.Text.Encodings.Web;
using Karamchari.Core.Multitenancy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Karamchari.Api.Security;

internal sealed class DevelopmentTenantAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>
    /// TODO: Add XML documentation.
    /// </summary>
    public const string SchemeName = "DevelopmentTenant";

    private readonly TenantOptions _tenantOptions;

    public DevelopmentTenantAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<TenantOptions> tenantOptions)
        : base(options, logger, encoder)
    {
        _tenantOptions = tenantOptions.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(_tenantOptions.TenantHeaderName, out var tenantHeader)
            || string.IsNullOrWhiteSpace(tenantHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (string.IsNullOrWhiteSpace(_tenantOptions.TrustedGatewayFingerprint))
        {
            return Task.FromResult(AuthenticateResult.Fail("Development gateway fingerprint is not configured."));
        }

        if (!Request.Headers.TryGetValue(_tenantOptions.TrustedGatewayHeader, out var proof)
            || !string.Equals(proof.ToString(), _tenantOptions.TrustedGatewayFingerprint, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail("Development gateway proof is invalid."));
        }

        var tenantId = tenantHeader.ToString().Trim().ToLowerInvariant();
        try
        {
            _ = new TenantContext(tenantId, TenantSource.TrustedHeader);
        }
        catch (ArgumentException ex)
        {
            return Task.FromResult(AuthenticateResult.Fail(ex.Message));
        }

        var claims = new[]
        {
            new Claim(_tenantOptions.JwtClaimType, tenantId),
            new Claim(ClaimTypes.NameIdentifier, $"local-dev:{tenantId}"),
            new Claim(ClaimTypes.Name, $"local-dev:{tenantId}"),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
