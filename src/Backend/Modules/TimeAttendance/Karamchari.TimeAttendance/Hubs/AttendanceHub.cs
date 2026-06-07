namespace Karamchari.TimeAttendance.Hubs;

using System.Security.Claims;
using Karamchari.Core.Multitenancy;
using Microsoft.AspNetCore.SignalR;

/// <summary>
/// Hub for live attendance monitoring. 
/// Clients subscribe by tenant ID to avoid cross-tenant data leakage and minimize broadcast noise.
/// Designed for Redis backplane compatibility.
/// </summary>
public sealed class AttendanceHub(ITenantProvider tenantProvider) : Hub
{
    private readonly ITenantProvider _tenantProvider = tenantProvider;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        TenantExecutionEnvelope tenant = _tenantProvider.GetTenant();
        if (tenant != null)
        {
            // Group by tenant to scale across multiple instances
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant:{tenant.TenantId}");
        }

        // Optional: Location-specific groups (e.g. site:{siteId}) could be added here
        // based on user claims.

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        TenantExecutionEnvelope tenant = _tenantProvider.GetTenant();
        if (tenant != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant:{tenant.TenantId}");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
