// -----------------------------------------------------------------------
// <copyright file="NullEmailProvider.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Karamchari.Notifications.Email;

/// <summary>
/// Phase 1 no-op email provider. Logs intent; returns success with a synthetic message ID.
/// Swap with SendGridEmailProvider or SesEmailProvider in Phase 2 via DI registration.
/// </summary>
public sealed class NullEmailProvider : IEmailProvider
{
    private readonly ILogger<NullEmailProvider> _logger;

    public NullEmailProvider(ILogger<NullEmailProvider> logger) => _logger = logger;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Task<EmailProviderResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug(
                "NullEmailProvider: would send email to {To} subject {Subject}",
                message.ToAddress, message.Subject);

        var syntheticId = $"null:{Guid.NewGuid():N}";
        return Task.FromResult(EmailProviderResult.Ok(syntheticId));
    }
}
