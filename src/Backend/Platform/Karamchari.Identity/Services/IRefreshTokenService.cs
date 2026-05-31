using Karamchari.Identity.Domain;

namespace Karamchari.Identity.Services;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public interface IRefreshTokenService
{
    /// <inheritdoc/>
    Task<(RefreshToken Token, string RawToken)> CreateTokenAsync(Guid userId, string tenantId, string? deviceInfo = null);
    /// <inheritdoc/>
    Task<(RefreshToken? Token, string? RawToken)> RotateTokenAsync(string rawToken, string? ipAddress = null);
    /// <inheritdoc/>
    Task RevokeTokenAsync(string rawToken, string? ipAddress = null);
    /// <inheritdoc/>
    Task RevokeAllUserTokensAsync(Guid userId, string tenantId);
}
