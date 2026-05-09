namespace Karamchari.Identity.Infrastructure.Configuration;

/// <summary>
/// Options for JWT token generation and validation.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Jwt";

    /// <summary>The secret key for signing tokens.</summary>
    public string Secret { get; init; } = string.Empty;
    
    /// <summary>The token issuer.</summary>
    public string Issuer { get; init; } = string.Empty;
    
    /// <summary>The token audience.</summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>The primary signing key ID.</summary>
    public string ActiveKeyId { get; init; } = "primary";

    /// <summary>Dictionary of valid signing keys by Key ID (kid).</summary>
    public Dictionary<string, string> SigningKeys { get; init; } = new();
    
    /// <summary>Access token expiration in minutes.</summary>
    public int AccessTokenExpirationMinutes { get; init; } = 60;
    
    /// <summary>Refresh token expiration in days.</summary>
    public int RefreshTokenExpirationDays { get; init; } = 7;
}
