namespace Karamchari.Identity.Domain;

/// <summary>
/// Stores JTI (JWT ID) of revoked access tokens.
/// </summary>
public sealed class RevokedToken
{
    public string Jti { get; private set; } = string.Empty;
    public DateTimeOffset RevokedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    private RevokedToken() { }

    public static RevokedToken Create(string jti, DateTimeOffset expiresAt)
    {
        return new RevokedToken
        {
            Jti = jti,
            RevokedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = expiresAt
        };
    }
}
