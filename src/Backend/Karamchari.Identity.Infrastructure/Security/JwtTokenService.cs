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

    /// <summary>Initializes a new instance of the <see cref="JwtTokenService"/> class.</summary>
    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>Generates an access token for the specified user.</summary>
    public string GenerateAccessToken(string tenantId, Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissions)
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

        // 1. Resolve signing key
        string secret = _options.Secret;
        string kid = _options.ActiveKeyId;

        if (_options.SigningKeys.TryGetValue(kid, out var keySecret))
        {
            secret = keySecret;
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
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
            null,
            DateTime.UtcNow.AddMinutes(_options.AccessTokenExpirationMinutes));

        var token = new JwtSecurityToken(header, payload);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Generates a refresh token.</summary>
    public (string RawToken, string Hash, DateTimeOffset ExpiresAt) GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        
        var raw = Convert.ToBase64String(randomNumber);
        var hash = HashToken(raw);

        return (raw, hash, DateTimeOffset.UtcNow.AddDays(_options.RefreshTokenExpirationDays));
    }

    public string HashToken(string rawToken)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(bytes);
    }

    /// <summary>Validates a JWT token.</summary>
    public bool ValidateToken(string token, out ClaimsPrincipal? principal)
    {
        principal = null;
        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            principal = tokenHandler.ValidateToken(token, GetValidationParameters(), out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private TokenValidationParameters GetValidationParameters()
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero
        };

        // Use IssuerSigningKeyResolver to find the right key by kid
        parameters.IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
        {
            var keys = new List<SecurityKey>();

            // 1. Check named keys
            if (!string.IsNullOrEmpty(kid) && _options.SigningKeys.TryGetValue(kid, out var secret))
            {
                keys.Add(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)));
            }
            else
            {
                // 2. Fallback to all known keys if kid missing or unknown
                foreach (var s in _options.SigningKeys.Values)
                {
                    keys.Add(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(s)));
                }
                
                // 3. Fallback to legacy Secret
                keys.Add(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret)));
            }

            return keys;
        };

        return parameters;
    }
}
