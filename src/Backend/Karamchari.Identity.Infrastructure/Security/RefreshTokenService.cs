using Karamchari.Identity.Domain;
using Karamchari.Identity.Infrastructure.Persistence;
using Karamchari.Identity.Services;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Identity.Infrastructure.Security;

internal sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly IdentityDbContext _db;
    private readonly IJwtTokenService _jwtService;
    private readonly ISecurityAuditService _auditService;

    public RefreshTokenService(
        IdentityDbContext db,
        IJwtTokenService jwtService,
        ISecurityAuditService auditService)
    {
        _db = db;
        _jwtService = jwtService;
        _auditService = auditService;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task<(RefreshToken Token, string RawToken)> CreateTokenAsync(Guid userId, string tenantId, string? deviceInfo = null)
    {
        var (raw, hash, expires) = _jwtService.GenerateRefreshToken();
        var token = RefreshToken.Create(hash, userId, tenantId, expires, deviceInfo);

        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync();

        return (token, raw);
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task<(RefreshToken? Token, string? RawToken)> RotateTokenAsync(string rawToken, string? ipAddress = null)
    {
        var hash = _jwtService.HashToken(rawToken);
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash);

        if (token == null || !token.IsActive)
        {
            if (token != null && (token.IsRevoked || token.IsExpired))
            {
                // REPLAY DETECTION: compromised token reused.
                if (token.IsRevoked)
                {
                    _auditService.LogSuspiciousActivity(
                        token.UserId.ToString(),
                        "REFRESH_TOKEN_REPLAY",
                        $"Token hash {hash} reused after revocation. Revoking family.");
                    await RevokeAllUserTokensAsync(token.UserId, token.TenantId);
                }
            }
            return (null, null);
        }

        // Generate new token
        var (newRaw, newHash, newExpires) = _jwtService.GenerateRefreshToken();
        var newToken = RefreshToken.Create(newHash, token.UserId, token.TenantId, newExpires, token.DeviceInfo);

        // Revoke old
        token.Revoke(ipAddress, newHash);

        _db.RefreshTokens.Add(newToken);
        await _db.SaveChangesAsync();

        return (newToken, newRaw);
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task RevokeTokenAsync(string rawToken, string? ipAddress = null)
    {
        var hash = _jwtService.HashToken(rawToken);
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);

        if (token != null && token.IsActive)
        {
            token.Revoke(ipAddress);
            await _db.SaveChangesAsync();
            _auditService.LogTokenRevocation(hash, "Explicit logout", token.UserId.ToString());
        }
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task RevokeAllUserTokensAsync(Guid userId, string tenantId)
    {
        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.TenantId == tenantId && t.RevokedAtUtc == null)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.Revoke();
        }

        await _db.SaveChangesAsync();
    }
}
