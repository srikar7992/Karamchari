namespace Karamchari.Identity.Services;

public interface ITokenBlacklistService
{
    Task RevokeTokenAsync(string jti, DateTimeOffset expiresAt);
    Task<bool> IsRevokedAsync(string jti);
}
