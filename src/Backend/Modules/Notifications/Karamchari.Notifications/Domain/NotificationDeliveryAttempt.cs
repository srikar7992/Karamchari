// -----------------------------------------------------------------------
// <copyright file="NotificationDeliveryAttempt.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Notifications.Domain;

/// <summary>
/// Append-only record of one delivery attempt for one channel.
/// Never updated â€” new attempt = new row. Supports retry auditing and bounce tracking.
/// </summary>
public sealed class NotificationDeliveryAttempt : Entity<Guid>
{
    private NotificationDeliveryAttempt() { /* EF materialization */ }

    internal NotificationDeliveryAttempt(
        Guid id,
        Guid notificationMessageId,
        NotificationChannel channel,
        DeliveryResult result,
        string? providerMessageId,
        string? failureReason,
        int attemptNumber) : base(id)
    {
        NotificationMessageId = notificationMessageId;
        Channel = channel;
        Result = result;
        ProviderMessageId = providerMessageId;
        FailureReason = failureReason;
        AttemptNumber = attemptNumber;
        AttemptedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid NotificationMessageId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public NotificationChannel Channel { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DeliveryResult Result { get; private set; }

    /// <summary>Message ID returned by the delivery provider (e.g., Azure Communication Services message ID).</summary>
    public string? ProviderMessageId { get; private set; }

    public string? FailureReason { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int AttemptNumber { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset AttemptedOnUtc { get; private set; }
}
