using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Karamchari.Core.Multitenancy;

/// <summary>
/// HTTP-bound implementation of <see cref="ITenantProvider"/>.
///
/// Resolution order (per the architecture charter):
/// <list type="number">
///   <item>JWT claim — authoritative for user-driven requests.</item>
///   <item>Trusted gateway header — accepted only when the gateway proof header is present and valid.</item>
///   <item>Host subdomain — used only to corroborate the JWT, never as the source of truth.</item>
/// </list>
///
/// If multiple sources are present they must agree; disagreement is an error.
/// There is no fallback — missing tenant context is fatal for any request that
/// reaches this provider.
/// </summary>
internal sealed class HttpTenantProvider : ITenantProvider
{
    private const string HttpItemsKey = "__Karamchari.Tenant__";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TenantOptions _options;
    private readonly ILogger<HttpTenantProvider> _logger;

    public HttpTenantProvider(
        IHttpContextAccessor httpContextAccessor,
        IOptions<TenantOptions> options,
        ILogger<HttpTenantProvider> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
        _logger = logger;
    }

    public TenantContext GetTenant()
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new TenantResolutionException(
                "No HttpContext is available. Background work must use IBackgroundTenantScope, not ITenantProvider.");

        // Cache the resolved tenant on HttpContext.Items so we resolve at most once per request.
        if (httpContext.Items.TryGetValue(HttpItemsKey, out var cached) && cached is TenantContext c)
        {
            return c;
        }

        var resolved = ResolveCore(httpContext);
        httpContext.Items[HttpItemsKey] = resolved;
        return resolved;
    }

    public bool TryGetTenant([NotNullWhen(true)] out TenantContext? tenant)
    {
        try
        {
            tenant = GetTenant();
            return true;
        }
        catch (TenantResolutionException)
        {
            tenant = null;
            return false;
        }
    }

    private TenantContext ResolveCore(HttpContext httpContext)
    {
        var fromJwt = ReadJwtTenant(httpContext.User);
        var fromHeader = ReadTrustedHeaderTenant(httpContext);
        var fromHost = ReadSubdomainTenant(httpContext);

        // For user requests (JWT present), the JWT is authoritative.
        if (fromJwt is not null)
        {
            EnforceAgreement(fromJwt, fromHeader, "trusted gateway header");
            EnforceAgreement(fromJwt, fromHost, "host subdomain");
            return new TenantContext(fromJwt, TenantSource.JwtClaim);
        }

        // For service-to-service calls without a JWT, only a *trusted* header is acceptable.
        if (fromHeader is not null)
        {
            EnforceAgreement(fromHeader, fromHost, "host subdomain");
            return new TenantContext(fromHeader, TenantSource.TrustedHeader);
        }

        // Subdomain alone is never sufficient.
        throw new TenantResolutionException(
            "No tenant could be resolved from the request. JWT tenant claim or trusted gateway header is required.",
            TenantResolutionFailureReason.MissingJwtClaim);
    }

    private string? ReadJwtTenant(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var raw = user.FindFirst(_options.JwtClaimType)?.Value;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        // Validate format eagerly so we surface a clean error here instead of deep in EF.
        var lower = raw.Trim().ToLowerInvariant();
        ValidateTenantId(lower, "JWT claim");
        return lower;
    }

    private string? ReadTrustedHeaderTenant(HttpContext httpContext)
    {
        if (!httpContext.Request.Headers.TryGetValue(_options.TenantHeaderName, out var raw)
            || string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        // Header is meaningful only when the gateway proof is present and matches.
        if (string.IsNullOrEmpty(_options.TrustedGatewayFingerprint))
        {
            _logger.LogWarning(
                "Tenant header {Header} present but TrustedGatewayFingerprint is not configured. Header ignored.",
                _options.TenantHeaderName);
            return null;
        }

        if (!httpContext.Request.Headers.TryGetValue(_options.TrustedGatewayHeader, out var proof)
            || !string.Equals(proof.ToString(), _options.TrustedGatewayFingerprint, StringComparison.Ordinal))
        {
            throw new TenantResolutionException(
                "Tenant header was provided without a valid gateway proof. Refusing to honour client-supplied tenant id.",
                TenantResolutionFailureReason.UntrustedHeaderSource);
        }

        var lower = raw.ToString().Trim().ToLowerInvariant();
        ValidateTenantId(lower, "trusted gateway header");
        return lower;
    }

    private string? ReadSubdomainTenant(HttpContext httpContext)
    {
        if (!_options.RequireSubdomainAgreement)
        {
            return null;
        }

        var host = httpContext.Request.Host.Host;
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        // Match against any configured suffix; pick the longest one that matches.
        var suffixes = _options.AppHostSuffixes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .OrderByDescending(s => s.Length);

        foreach (var suffix in suffixes)
        {
            if (!host.EndsWith("." + suffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var sub = host[..^(suffix.Length + 1)];
            if (string.IsNullOrEmpty(sub))
            {
                return null;
            }

            // Only the leftmost label is the tenant; everything else (e.g., 'api.acme') is invalid.
            var tenantLabel = sub.Split('.', 2)[0];
            var lower = tenantLabel.Trim().ToLowerInvariant();
            ValidateTenantId(lower, "host subdomain");
            return lower;
        }

        return null;
    }

    private static void EnforceAgreement(string canonical, string? other, string otherSourceName)
    {
        if (other is null)
        {
            return;
        }

        if (!string.Equals(canonical, other, StringComparison.Ordinal))
        {
            throw new TenantResolutionException(
                $"Tenant disagreement: JWT/header indicates '{canonical}' but {otherSourceName} indicates '{other}'.",
                TenantResolutionFailureReason.SourcesDisagree);
        }
    }

    private static void ValidateTenantId(string candidate, string sourceName)
    {
        try
        {
            // Constructing a TenantContext validates the format; we discard the result here.
            _ = new TenantContext(candidate, TenantSource.JwtClaim);
        }
        catch (ArgumentException ex)
        {
            throw new TenantResolutionException(
                $"Tenant id from {sourceName} is invalid: {ex.Message}",
                TenantResolutionFailureReason.InvalidTenantIdFormat);
        }
    }
}
