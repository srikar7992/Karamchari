namespace Karamchari.TimeAttendance.Services;

using Karamchari.TimeAttendance.Hubs;
using Karamchari.TimeAttendance.Domain.IoT;
using Microsoft.AspNetCore.SignalR;

/// <summary>
/// Broadcasts live attendance updates to the real-time dashboard.
/// Called by the PunchTranslationConsumer.
/// </summary>
public sealed class LiveAttendanceService
{
    private readonly IHubContext<AttendanceHub> _hubContext;

    public LiveAttendanceService(IHubContext<AttendanceHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task BroadcastUpdateAsync(LiveAttendance state, CancellationToken ct = default)
    {
        // Broadcasts to the specific tenant group to prevent cross-tenant data leakage
        await _hubContext.Clients.Group($"tenant:{state.TenantId}")
            .SendAsync("attendanceUpdated", new
            {
                state.EmployeeId,
                state.IsPresent,
                state.Status,
                state.LastCheckIn,
                state.LastCheckOut,
                state.LastUpdatedAtUtc
            }, ct);
    }

    public async Task BroadcastRawAsync(dynamic payload, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group($"tenant:{payload.TenantId}")
            .SendAsync("attendanceUpdated", payload, ct);
    }
}
