namespace Karamchari.TimeAttendance.Consumers;

using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.TimeAttendance.Domain.IoT;
using Karamchari.TimeAttendance.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Consumes raw punches from the edge, parses them, resolves EmployeeId,
/// enforces strict idempotency, performs inline fraud detection, and saves the source of truth.
/// </summary>
public sealed class PunchTranslationConsumer : IConsumer<RawPunchReceivedEvent>
{
    private readonly TimeAttendanceDbContext _db;
    private readonly IPublishEndpoint _publishEndpoint;

    public PunchTranslationConsumer(TimeAttendanceDbContext db, IPublishEndpoint publishEndpoint)
    {
        _db = db;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<RawPunchReceivedEvent> context)
    {
        var msg = context.Message;
        using var transaction = await _db.Database.BeginTransactionAsync(context.CancellationToken);

        try
        {
            var payloadStr = msg.Payload;
            var parsed = JsonSerializer.Deserialize<JsonElement>(payloadStr);

            string externalId;
            if (parsed.TryGetProperty("externalId", out var extIdProp))
            {
                externalId = extIdProp.GetString() ?? GenerateHash(msg.DeviceId, payloadStr);
            }
            else
            {
                externalId = GenerateHash(msg.DeviceId, payloadStr);
            }

            // 1. Strict Idempotency Check (Layer 1 & Layer 2)
            var exists = await _db.RawPunches.AnyAsync(
                p => p.DeviceId == msg.DeviceId && p.ExternalId == externalId, 
                context.CancellationToken);

            if (exists)
            {
                // Already processed, acknowledge and return
                return;
            }

            // 2. Parse required fields
            if (!parsed.TryGetProperty("biometricId", out var bioIdProp) || !parsed.TryGetProperty("timestamp", out var tsProp))
            {
                await HandleInvalidAsync(msg, "Missing biometricId or timestamp");
                await transaction.CommitAsync();
                return;
            }

            var biometricId = bioIdProp.GetString()!;
            var rawTimestampStr = tsProp.GetString()!;
            if (!DateTime.TryParse(rawTimestampStr, out var rawTimestamp))
            {
                await HandleInvalidAsync(msg, "Invalid timestamp format");
                await transaction.CommitAsync();
                return;
            }

            // 3. Resolve EmployeeId
            var mapping = await _db.BiometricMappings.FirstOrDefaultAsync(
                m => m.BiometricId == biometricId && m.TenantId == msg.TenantId, 
                context.CancellationToken);

            if (mapping == null)
            {
                await HandleInvalidAsync(msg, $"Unmapped BiometricId: {biometricId}");
                await transaction.CommitAsync();
                return;
            }

            // 4. Timezone normalizations
            var timeZoneStr = parsed.TryGetProperty("timeZone", out var tzProp) ? tzProp.GetString() : "UTC";
            // Assume the device sends ISO8601 with offset, so ToUniversalTime is safe.
            var timestampUtc = rawTimestamp.ToUniversalTime(); 
            var localTimestamp = rawTimestamp; // Keeping it simple for the sprint

            // 5. Parse Type and Source
            var punchType = PunchType.Unknown;
            if (parsed.TryGetProperty("type", out var ptProp))
            {
                var ptStr = ptProp.GetString()?.ToLowerInvariant();
                punchType = ptStr == "in" ? PunchType.In : (ptStr == "out" ? PunchType.Out : PunchType.Unknown);
            }

            double? lat = null, lon = null;
            if (msg.IsMobileSource)
            {
                if (parsed.TryGetProperty("latitude", out var latProp)) lat = latProp.GetDouble();
                if (parsed.TryGetProperty("longitude", out var lonProp)) lon = lonProp.GetDouble();
            }

            // 6. Create Domain Entity
            var rawPunch = RawPunch.Create(
                msg.TenantId,
                mapping.EmployeeId,
                msg.DeviceId,
                externalId,
                timestampUtc,
                localTimestamp,
                timeZoneStr ?? "UTC",
                msg.ReceivedAtUtc,
                punchType,
                msg.IsMobileSource ? PunchSource.Mobile : PunchSource.Biometric,
                lat,
                lon
            );

            // 7. Inline Fraud Detection (e.g. GPS spoofing, rapid duplicate)
            await InlineFraudCheckAsync(rawPunch, context.CancellationToken);

            _db.RawPunches.Add(rawPunch);

            // 8. Trigger Live Attendance Service (Synchronous update to dashboard)
            await UpdateLiveAttendanceAsync(rawPunch, context.CancellationToken);

            await _db.SaveChangesAsync(context.CancellationToken);
            await transaction.CommitAsync();
        }
        catch (JsonException ex)
        {
            await HandleInvalidAsync(msg, $"JSON Parse Error: {ex.Message}");
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw; // Let MassTransit retry or move to standard DLQ
        }
    }

    private async Task HandleInvalidAsync(RawPunchReceivedEvent msg, string reason)
    {
        var invalid = InvalidPunch.Create(msg.Payload, reason, msg.DeviceId);
        _db.InvalidPunches.Add(invalid);
        await _db.SaveChangesAsync();
    }

    private string GenerateHash(string deviceId, string payload)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(deviceId + payload));
        return Convert.ToBase64String(hash);
    }

    private async Task InlineFraudCheckAsync(RawPunch punch, CancellationToken ct)
    {
        // Check for rapid fire duplicate (same employee, < 5 seconds)
        var recent = await _db.RawPunches
            .Where(p => p.EmployeeId == punch.EmployeeId && p.DeviceId == punch.DeviceId)
            .OrderByDescending(p => p.TimestampUtc)
            .FirstOrDefaultAsync(ct);

        if (recent != null)
        {
            var diff = (punch.TimestampUtc - recent.TimestampUtc).TotalSeconds;
            if (Math.Abs(diff) < 5)
            {
                var fraud = FraudFlag.Create(
                    punch.TenantId,
                    punch.EmployeeId,
                    punch.Id,
                    FraudType.BuddyPunching, // or RapidDuplicate
                    $"Punch registered {diff:F1}s after previous punch on same device.",
                    40);
                _db.FraudFlags.Add(fraud);
            }
        }
    }

    private async Task UpdateLiveAttendanceAsync(RawPunch punch, CancellationToken ct)
    {
        var liveState = await _db.LiveAttendance
            .FirstOrDefaultAsync(x => x.EmployeeId == punch.EmployeeId, ct);

        if (liveState == null)
        {
            liveState = LiveAttendance.Create(punch.TenantId, punch.EmployeeId);
            _db.LiveAttendance.Add(liveState);
        }

        if (punch.Type == PunchType.In)
        {
            // For true 'Late' logic we need the Shift. Simplification for inline:
            liveState.UpdateCheckIn(punch.TimestampUtc, isLate: false);
        }
        else if (punch.Type == PunchType.Out)
        {
            liveState.UpdateCheckOut(punch.TimestampUtc);
        }
        
        await _publishEndpoint.Publish(new LiveAttendanceUpdatedEventV1
        {
            TenantId = liveState.TenantId,
            EmployeeId = liveState.EmployeeId,
            IsPresent = liveState.IsPresent,
            Status = liveState.Status,
            LastCheckIn = liveState.LastCheckIn,
            LastCheckOut = liveState.LastCheckOut,
            LastUpdatedAtUtc = liveState.LastUpdatedAtUtc
        }, ct);
    }
}
