using System.Security.Claims;

namespace Karamchari.Identity.Services;

/// <summary>
/// Service for generating and validating JWT tokens.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>Generates an access token for the specified user.</summary>
    Task<string> GenerateAccessTokenAsync(string tenantId, Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissions, string permissionVersion);

    /// <summary>Generates a raw refresh token and its hash.</summary>
    (string RawToken, string Hash, DateTimeOffset ExpiresAt) GenerateRefreshToken();

    /// <summary>Hashes a raw token for comparison.</summary>
    string HashToken(string rawToken);

    /// <summary>Validates a JWT token.</summary>
    bool ValidateToken(string token, out ClaimsPrincipal? principal);
}
