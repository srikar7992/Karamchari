namespace Karamchari.TimeAttendance.Edge;

using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;
using Karamchari.TimeAttendance.Domain.IoT;

public sealed record DeviceAuthResult(bool IsValid, Device? Device);

/// <summary>
/// Extremely fast device authentication service. Validates API keys and resolves the tenant context.
/// Runs in the Edge Ingestion API.
/// </summary>
public sealed class DeviceAuthService
{
    private readonly TimeAttendanceDbContext _db;

    public DeviceAuthService(TimeAttendanceDbContext db)
    {
        _db = db;
    }

    public async Task<DeviceAuthResult> ValidateAsync(string apiKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return new DeviceAuthResult(false, null);

        // Note: Global query filters (RLS) might be bypassed here or explicitly handled,
        // because the edge request doesn't yet have a JWT with a TenantId. 
        // We look up the device purely by its unique API key.
        var device = await _db.Devices
            .IgnoreQueryFilters() // Bypass RLS to resolve the tenant from the key
            .FirstOrDefaultAsync(d => d.ApiKey == apiKey && d.IsActive, ct);

        return new DeviceAuthResult(device != null, device);
    }
}
