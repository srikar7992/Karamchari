// -----------------------------------------------------------------------
// <copyright file="RefreshToken.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Identity.Domain;

/// <summary>
/// Persistent refresh token for session management.
/// Stores hash only for security.
/// </summary>
public sealed class RefreshToken
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid Id { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TokenHash { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid UserId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    /// <inheritdoc/>
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    /// <inheritdoc/>
    public string? RevokedByIp { get; private set; }
    /// <inheritdoc/>
    public string? ReplacedByTokenHash { get; private set; }
    /// <inheritdoc/>
    public string? DeviceInfo { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAtUtc;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsRevoked => RevokedAtUtc != null;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsActive => !IsRevoked && !IsExpired;

    private RefreshToken() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Revoke(string? ip = null, string? replacedByHash = null)
    {
        RevokedAtUtc = DateTimeOffset.UtcNow;
        RevokedByIp = ip;
        ReplacedByTokenHash = replacedByHash;
    }
}
