// -----------------------------------------------------------------------
// <copyright file="INotificationOrchestrator.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Notifications.Orchestration;

/// <summary>
/// Single entry point for all notification delivery.
/// Consumers create a <see cref="NotificationIntent"/> and call this interface;
/// all governance (dedupe, preferences, template, channel fanout, real-time push)
/// happens inside the implementation.
/// </summary>
public interface INotificationOrchestrator
{
    Task OrchestrateAsync(NotificationIntent intent, CancellationToken cancellationToken = default);
}
