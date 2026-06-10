// -----------------------------------------------------------------------
// <copyright file="AnalyticsBroadcaster.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.PSA.Hubs;

using Microsoft.AspNetCore.SignalR;

/// <summary>
/// Broadcasts analytics updates to SignalR subscribers.
/// Called by the ProfitCalculationConsumer AFTER aggregation, never from the raw event.
///
/// Pattern: push aggregated data only â†’ low bandwidth, high signal.
/// </summary>
public sealed class AnalyticsBroadcaster
{
    private readonly IHubContext<AnalyticsHub> _hub;

    public AnalyticsBroadcaster(IHubContext<AnalyticsHub> hub)
    {
        _hub = hub;
    }

    /// <summary>
    /// Pushes an updated project metrics snapshot to all subscribers.
    /// </summary>
    public async Task BroadcastProjectUpdateAsync(
        Guid projectId,
        decimal revenue,
        decimal cost,
        decimal profit,
        decimal margin,
        decimal utilization)
    {
        var payload = new
        {
            projectId,
            revenue,
            cost,
            profit,
            margin,
            utilization,
            timestamp = DateTime.UtcNow
        };

        // Push to project-specific subscribers.
        await _hub.Clients
            .Group($"project-{projectId}")
            .SendAsync("projectUpdated", payload);

        // Push to dashboard subscribers (global feed).
        await _hub.Clients
            .Group("dashboard")
            .SendAsync("projectUpdated", payload);
    }

    /// <summary>
    /// Pushes an anomaly alert to the dashboard.
    /// </summary>
    public async Task BroadcastAnomalyAsync(
        Guid projectId,
        string anomalyType,
        string message,
        string severity)
    {
        await _hub.Clients
            .Group("dashboard")
            .SendAsync("anomalyDetected", new
            {
                projectId,
                anomalyType,
                message,
                severity,
                timestamp = DateTime.UtcNow
            });
    }
}
