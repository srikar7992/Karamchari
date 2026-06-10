// -----------------------------------------------------------------------
// <copyright file="ITokenBlacklistService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Identity.Services;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public interface ITokenBlacklistService
{
    /// <inheritdoc/>
    Task RevokeTokenAsync(string jti, DateTimeOffset expiresAt);
    /// <inheritdoc/>
    Task<bool> IsRevokedAsync(string jti);
}
