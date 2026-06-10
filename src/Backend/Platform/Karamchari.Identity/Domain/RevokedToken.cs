// -----------------------------------------------------------------------
// <copyright file="RevokedToken.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Identity.Domain;

/// <summary>
/// Stores JTI (JWT ID) of revoked access tokens.
/// </summary>
public sealed class RevokedToken
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Jti { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset RevokedAtUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    private RevokedToken() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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
