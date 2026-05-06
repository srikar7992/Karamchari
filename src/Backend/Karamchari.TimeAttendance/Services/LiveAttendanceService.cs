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
        ArgumentNullException.ThrowIfNull(state);
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

    public async Task BroadcastRawAsync(object payload, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        string tenantId = ((dynamic)payload).TenantId;
        await _hubContext.Clients.Group($"tenant:{tenantId}")
            .SendAsync("attendanceUpdated", payload, ct);
    }
}
