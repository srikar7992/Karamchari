using Karamchari.Identity.Domain;

namespace Karamchari.Identity.Services;

public interface IRefreshTokenService
{
    Task<(RefreshToken Token, string RawToken)> CreateTokenAsync(Guid userId, string tenantId, string? deviceInfo = null);
    Task<(RefreshToken? Token, string? RawToken)> RotateTokenAsync(string rawToken, string? ipAddress = null);
    Task RevokeTokenAsync(string rawToken, string? ipAddress = null);
    Task RevokeAllUserTokensAsync(Guid userId, string tenantId);
}
