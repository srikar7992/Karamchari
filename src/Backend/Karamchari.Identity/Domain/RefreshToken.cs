namespace Karamchari.Identity.Domain;

/// <summary>
/// Persistent refresh token for session management.
/// Stores hash only for security.
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public string? DeviceInfo { get; private set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAtUtc;
    public bool IsRevoked => RevokedAtUtc != null;
    public bool IsActive => !IsRevoked && !IsExpired;

    private RefreshToken() { }

    public static RefreshToken Create(
        string tokenHash, 
        Guid userId, 
        string tenantId, 
        DateTimeOffset expiresAt,
        string? deviceInfo = null)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = tokenHash,
            UserId = userId,
            TenantId = tenantId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = expiresAt,
            DeviceInfo = deviceInfo
        };
    }

    public void Revoke(string? ip = null, string? replacedByHash = null)
    {
        RevokedAtUtc = DateTimeOffset.UtcNow;
        RevokedByIp = ip;
        ReplacedByTokenHash = replacedByHash;
    }
}
