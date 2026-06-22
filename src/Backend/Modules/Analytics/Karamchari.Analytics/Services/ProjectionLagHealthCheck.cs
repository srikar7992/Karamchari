using Karamchari.Analytics.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Karamchari.Analytics.Services;

/// <summary>
/// Healthy: last projection less than 26h ago (WorkforceSnapshotJob runs nightly; 26h = one cycle + buffer).
/// Degraded: 26-48h (missed one cycle — investigate).
/// Unhealthy: more than 48h (two missed cycles — pipeline stalled).
/// </summary>
public sealed class ProjectionLagHealthCheck(AnalyticsDbContext db) : IHealthCheck
{
    private static readonly TimeSpan DegradedThreshold = TimeSpan.FromHours(26);
    private static readonly TimeSpan UnhealthyThreshold = TimeSpan.FromHours(48);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var lastUpdate = await db.DimEmployees
            .IgnoreQueryFilters()
            .MaxAsync(e => (DateTimeOffset?)e.LastUpdatedUtc, cancellationToken);

        if (lastUpdate is null)
        {
            var empty = new Dictionary<string, object> { ["last_projection_utc"] = "never" };
            return HealthCheckResult.Degraded("No analytics projections recorded yet.", null, empty);
        }

        var age = DateTimeOffset.UtcNow - lastUpdate.Value;
        var data = new Dictionary<string, object>
        {
            ["last_projection_utc"] = lastUpdate.Value.ToString("O"),
            ["age_hours"] = Math.Round(age.TotalHours, 1)
        };

        if (age < DegradedThreshold)
            return HealthCheckResult.Healthy($"Last projection {Math.Round(age.TotalHours, 1)}h ago.", data);
        if (age < UnhealthyThreshold)
            return HealthCheckResult.Degraded($"Analytics lag {Math.Round(age.TotalHours, 1)}h — missed one cycle.", null, data);
        return HealthCheckResult.Unhealthy($"Analytics lag {Math.Round(age.TotalHours, 1)}h — pipeline stalled.", null, data);
    }
}
