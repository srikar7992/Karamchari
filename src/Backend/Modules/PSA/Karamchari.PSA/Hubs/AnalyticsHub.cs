// -----------------------------------------------------------------------
// <copyright file="AnalyticsHub.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.PSA.Hubs;

using Microsoft.AspNetCore.SignalR;

/// <summary>
/// SignalR hub for real-time analytics updates.
/// CFOs subscribe to project groups and receive live metric pushes
/// after every aggregation update â€” no polling, no page refresh.
///
/// Architecture: Event â†’ Aggregation â†’ Broadcaster â†’ this Hub â†’ UI
/// NOT: Event â†’ this Hub (that would flood connections).
/// </summary>
public sealed class AnalyticsHub : Hub
{
    /// <summary>
    /// Subscribes the caller to a specific project's real-time updates.
    /// </summary>
    public async Task SubscribeProject(Guid projectId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"project-{projectId}");
    }

    /// <summary>
    /// Unsubscribes the caller from a project's updates.
    /// </summary>
    public async Task UnsubscribeProject(Guid projectId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"project-{projectId}");
    }

    /// <summary>
    /// Subscribes the caller to the global dashboard feed (all projects).
    /// </summary>
    public async Task SubscribeDashboard()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "dashboard");
    }
}
