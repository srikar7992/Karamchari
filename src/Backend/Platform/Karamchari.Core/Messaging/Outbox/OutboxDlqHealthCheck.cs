using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Karamchari.Core.Messaging.Outbox;

/// <summary>
/// Healthy: DLQ empty.
/// Degraded: 1-9 messages (investigation warranted, not yet critical).
/// Unhealthy: 10+ messages (processing pipeline is shedding load).
/// </summary>
public sealed class OutboxDlqHealthCheck(OutboxRelayDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var count = await db.DeadLetterMessages.CountAsync(cancellationToken);
        var data = new Dictionary<string, object> { ["dlq_count"] = count };

        return count switch
        {
            0 => HealthCheckResult.Healthy("DLQ empty.", data),
            < 10 => HealthCheckResult.Degraded($"DLQ has {count} message(s) — investigate.", null, data),
            _ => HealthCheckResult.Unhealthy($"DLQ has {count} message(s) — pipeline shedding load.", null, data)
        };
    }
}
