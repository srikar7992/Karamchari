using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Karamchari.SecurityTests;

/// <summary>
/// D1 + D2 — JWT tampering and expiry tests.
/// All tests use the real JWT middleware (no test auth handler) so that tampering
/// is caught by signature validation.
/// </summary>
public sealed class JwtSecurityTests : IClassFixture<SecurityWebApplicationFactory>
{
    private readonly SecurityWebApplicationFactory _factory;

    // The signing secret from appsettings.json (Development default).
    // In CI, override via ASPNETCORE_Jwt__Secret environment variable.
    private const string KnownSecret =
        "REPLACE_VIA_ENV_OR_USER_SECRETS_IN_PRODUCTION_change_me_min_64_bytes_xxxxxxxx";

    private const string WrongSecret =
        "WRONG_SECRET_that_is_also_at_least_64_bytes_long_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";

    private const string Issuer = "https://karamchari.com";
    private const string Audience = "https://karamchari.com/api";

    public JwtSecurityTests(SecurityWebApplicationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    // ---- D1: Tampered JWT claims ----

    [Fact]
    public async Task D1_TamperedTenantId_Returns401()
    {
        // Build a valid token for tenant "acme"
        var validToken = BuildToken(
            tenantId: "acme",
            userId: Guid.NewGuid().ToString(),
            roles: ["Employee"],
            secret: KnownSecret);

        // Decode, modify the tenant_id claim, re-sign with a WRONG key
        var tamperedToken = TamperClaim(validToken, "tenant_id", "evil-corp", WrongSecret);

        var client = _factory.CreateUnauthenticatedClient("acme");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tamperedToken);

        var response = await client.GetAsync("/api/v1/hr/employees");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "a JWT with a tampered tenant_id claim re-signed with the wrong key must be rejected");
    }

    [Fact]
    public async Task D1_TamperedUserId_Returns401()
    {
        var validToken = BuildToken(
            tenantId: "acme",
            userId: Guid.NewGuid().ToString(),
            roles: ["Employee"],
            secret: KnownSecret);

        // Tamper sub claim and re-sign with wrong key
        var tamperedToken = TamperClaim(validToken, JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString(), WrongSecret);

        var client = _factory.CreateUnauthenticatedClient("acme");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tamperedToken);

        var response = await client.GetAsync("/api/v1/hr/employees");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "a JWT with a tampered sub (userId) claim re-signed with the wrong key must be rejected");
    }

    [Fact]
    public async Task D1_TamperedRoles_Returns401()
    {
        var validToken = BuildToken(
            tenantId: "acme",
            userId: Guid.NewGuid().ToString(),
            roles: ["Employee"],
            secret: KnownSecret);

        // Escalate role to Admin and re-sign with wrong key
        var tamperedToken = TamperClaim(validToken, ClaimTypes.Role, "Admin", WrongSecret);

        var client = _factory.CreateUnauthenticatedClient("acme");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tamperedToken);

        var response = await client.GetAsync("/api/v1/hr/employees");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "a JWT with tampered roles re-signed with the wrong key must be rejected");
    }

    // ---- D2: Expired token ----

    [Fact]
    public async Task D2_ExpiredToken_Returns401()
    {
        var expiredToken = BuildToken(
            tenantId: "acme",
            userId: Guid.NewGuid().ToString(),
            roles: ["Employee"],
            secret: KnownSecret,
            notBefore: DateTime.UtcNow.AddHours(-2),
            expires: DateTime.UtcNow.AddHours(-1)); // already expired

        var client = _factory.CreateUnauthenticatedClient("acme");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await client.GetAsync("/api/v1/hr/employees");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "an expired JWT must not grant access");
    }

    // ---- Helpers ----

    private static string BuildToken(
        string tenantId,
        string userId,
        string[] roles,
        string secret,
        DateTime? notBefore = null,
        DateTime? expires = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("tenant_id", tenantId),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: notBefore ?? DateTime.UtcNow,
            expires: expires ?? DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Decodes a JWT, replaces the first occurrence of <paramref name="claimType"/>
    /// with <paramref name="newValue"/>, and re-signs with <paramref name="resignSecret"/>.
    /// The original signature is invalidated because either the secret or payload changes.
    /// </summary>
    private static string TamperClaim(string originalToken, string claimType, string newValue, string resignSecret)
    {
        var handler = new JwtSecurityTokenHandler();
        var parsed = handler.ReadJwtToken(originalToken);

        var claims = parsed.Claims
            .Where(c => c.Type != claimType)
            .ToList();
        claims.Add(new Claim(claimType, newValue));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(resignSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tampered = new JwtSecurityToken(
            issuer: parsed.Issuer,
            audience: parsed.Audiences.FirstOrDefault(),
            claims: claims,
            notBefore: parsed.ValidFrom,
            expires: parsed.ValidTo,
            signingCredentials: creds);

        return handler.WriteToken(tampered);
    }
}
