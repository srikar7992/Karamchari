using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Karamchari.Identity.Infrastructure.Configuration;
using Karamchari.Identity.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Karamchari.Identity.Infrastructure.Security;

/// <summary>
/// Implementation of IJwtTokenService using System.IdentityModel.Tokens.Jwt.
/// </summary>
internal sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly DatabaseSigningKeyResolver _keyResolver;

    /// <summary>Initializes a new instance of the <see cref="JwtTokenService"/> class.</summary>
    public JwtTokenService(IOptions<JwtOptions> options, DatabaseSigningKeyResolver keyResolver)
    {
        _options = options.Value;
        _keyResolver = keyResolver;
    }

    /// <summary>Generates an access token for the specified user.</summary>
    public async Task<string> GenerateAccessTokenAsync(string tenantId, Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("tenant_id", tenantId)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        // 1. Resolve current signing key from DB (with fallback to config for dev/initial setup)
        var key = await _keyResolver.GetCurrentSigningKeyAsync();
        string kid = key?.KeyId ?? _options.ActiveKeyId;

        if (key == null)
        {
            string secret = _options.Secret;
            if (_options.SigningKeys.TryGetValue(kid, out var keySecret))
            {
                secret = keySecret;
            }
            key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        }

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 2. Add Key ID (kid) to header for rotation support
        var header = new JwtHeader(creds)
        {
            ["kid"] = kid
        };

        var payload = new JwtPayload(
            _options.Issuer,
            _options.Audience,
            claims,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(_options.AccessTokenExpirationMinutes));

        var token = new JwtSecurityToken(header, payload);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Generates a raw refresh token and its hash.</summary>
    public (string RawToken, string Hash, DateTimeOffset ExpiresAt) GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        var rawToken = Convert.ToBase64String(randomNumber);
        var hash = HashToken(rawToken);
        var expiresAt = DateTimeOffset.UtcNow.AddDays(_options.RefreshTokenExpirationDays);
        return (rawToken, hash, expiresAt);
    }

    /// <summary>Hashes a raw token for comparison.</summary>
    public string HashToken(string rawToken)
    {
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    /// <summary>Validates a JWT token.</summary>
    public bool ValidateToken(string token, out ClaimsPrincipal? principal)
    {
        principal = null;
        var handler = new JwtSecurityTokenHandler();

        try
        {
            // Note: Validation typically uses the keys configured in Authentication middleware.
            // This manual validation should use the same resolver logic if needed.
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _options.Issuer,
                ValidateAudience = true,
                ValidAudience = _options.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero,
                // In a real implementation, we would pass the resolver's keys here
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret))
            };

            principal = handler.ValidateToken(token, validationParameters, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
