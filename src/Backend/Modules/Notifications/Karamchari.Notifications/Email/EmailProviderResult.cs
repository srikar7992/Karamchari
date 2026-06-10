// -----------------------------------------------------------------------
// <copyright file="EmailProviderResult.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Notifications.Email;

/// <summary>Result returned by IEmailProvider.SendAsync.</summary>
public sealed record EmailProviderResult(
    bool Success,
    string? ProviderMessageId,
    string? FailureReason)
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static EmailProviderResult Ok(string providerMessageId) =>
        new(true, providerMessageId, null);

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static EmailProviderResult Fail(string reason) =>
        new(false, null, reason);
}
