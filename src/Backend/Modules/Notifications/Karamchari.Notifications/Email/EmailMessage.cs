// -----------------------------------------------------------------------
// <copyright file="EmailMessage.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Notifications.Email;

/// <summary>Payload passed to IEmailProvider.SendAsync.</summary>
public sealed record EmailMessage(
    string ToAddress,
    string ToDisplayName,
    string Subject,
    string BodyHtml,
    string BodyText,
    string? ReplyToAddress = null,
    string? IdempotencyKey = null);
