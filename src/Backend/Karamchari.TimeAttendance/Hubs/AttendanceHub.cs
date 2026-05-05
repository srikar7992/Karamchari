namespace Karamchari.TimeAttendance.Hubs;

using Microsoft.AspNetCore.SignalR;
using Karamchari.Core.Multitenancy;
using System.Security.Claims;

/// <summary>
/// Hub for live attendance monitoring. 
/// Clients subscribe by tenant ID to avoid cross-tenant data leakage and minimize broadcast noise.
/// Designed for Redis backplane compatibility.
/// </summary>
public sealed class AttendanceHub : Hub
{
    private readonly ITenantProvider _tenantProvider;

    public AttendanceHub(ITenantProvider tenantProvider)
    {
        _tenantProvider = tenantProvider;
    }

    public override async Task OnConnectedAsync()
    {
        var tenant = _tenantProvider.GetTenant();
        if (tenant != null)
        {
            // Group by tenant to scale across multiple instances
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant:{tenant.TenantId}");
        }

        // Optional: Location-specific groups (e.g. site:{siteId}) could be added here
        // based on user claims.

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var tenant = _tenantProvider.GetTenant();
        if (tenant != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant:{tenant.TenantId}");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
