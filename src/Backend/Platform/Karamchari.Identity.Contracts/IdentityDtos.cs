// -----------------------------------------------------------------------
// <copyright file="IdentityDtos.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Identity.Contracts;

/// <summary>Request to register a new user.</summary>
public record RegisterRequest(string Email, string Password, string FullName, string TenantId);

/// <summary>Request to log in.</summary>
public record LoginRequest(string Email, string Password);

/// <summary>Response containing authentication tokens.</summary>
public record LoginResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);

/// <summary>Request to refresh an access token.</summary>
public record RefreshTokenRequest(string RefreshToken);
