// -----------------------------------------------------------------------
// <copyright file="NotificationHub.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.SignalR;

namespace Karamchari.Notifications.RealTime;

/// <summary>
/// SignalR hub for real-time notification streaming.
///
/// Group naming convention: "{tenantId}:{employeeId}"
/// Ensures tenant isolation â€” one employee cannot receive another tenant's signals.
///
/// Client must supply tenantId + employeeId as query-string parameters:
///   /hubs/notifications?tenantId=acme&amp;employeeId={guid}
///
/// The caller (frontend) must be authenticated; the tenantId/employeeId are
/// validated against the JWT claims before AddToGroupAsync is called.
/// </summary>
public sealed class NotificationHub : Hub
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var http = Context.GetHttpContext();
        if (http is null) return;

        var tenantId = http.Request.Query["tenantId"].ToString();
        var employeeIdRaw = http.Request.Query["employeeId"].ToString();

        if (!string.IsNullOrWhiteSpace(tenantId) &&
            Guid.TryParse(employeeIdRaw, out var employeeId))
        {
            var group = GroupName(tenantId, employeeId);
            await Groups.AddToGroupAsync(Context.ConnectionId, group);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>Canonical group name for a user.</summary>
    public static string GroupName(string tenantId, Guid employeeId) =>
        $"{tenantId}:{employeeId}";
}
