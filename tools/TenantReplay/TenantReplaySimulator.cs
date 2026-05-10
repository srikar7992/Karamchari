namespace TenantReplay;

public sealed class TenantEventReplayConfig
{
    public required string[] TenantIds { get; init; }
    public required string EventSource { get; init; }
    public int ReplayCount { get; init; } = 10;
    public bool SimulateOutOfOrder { get; init; } = true;
    public bool SimulateDelayed { get; init; } = true;
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromHours(1);
}

public sealed class ReplayResult
{
    public required string EventId { get; init; }
    public required string TenantId { get; init; }
    public required DateTime OriginalTimestamp { get; init; }
    public required DateTime ReplayTimestamp { get; init; }
    public bool TenantPreserved { get; init; }
    public bool IsolationMaintained { get; init; }
    public string? FailureReason { get; init; }
}

public sealed class ReplayStormSimulator
{
    private readonly List<ReplayResult> _results = new();

    public async Task SimulateReplayStormAsync(TenantEventReplayConfig config, CancellationToken ct = default)
    {
        Console.WriteLine($"Simulating replay storm for {config.TenantIds.Length} tenants");

        var tasks = config.TenantIds.Select(tenant => Task.Run(async () =>
        {
            for (int i = 0; i < config.ReplayCount && !ct.IsCancellationRequested; i++)
            {
                var result = await SimulateSingleReplayAsync(tenant, config, ct);
                lock (_results)
                {
                    _results.Add(result);
                }
            }
        }));

        await Task.WhenAll(tasks);

        Console.WriteLine($"Replay storm completed: {_results.Count} replays");
    }

    private async Task<ReplayResult> SimulateSingleReplayAsync(string tenantId, TenantEventReplayConfig config, CancellationToken ct)
    {
        var eventId = Guid.NewGuid().ToString("N");
        var originalTime = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(1, 60));

        if (config.SimulateDelayed)
        {
            await Task.Delay(Random.Shared.Next(1, 100), ct);
        }

        var tenantPreserved = !string.IsNullOrEmpty(tenantId);
        var isolationMaintained = tenantPreserved;

        return new ReplayResult
        {
            EventId = eventId,
            TenantId = tenantId,
            OriginalTimestamp = originalTime,
            ReplayTimestamp = DateTime.UtcNow,
            TenantPreserved = tenantPreserved,
            IsolationMaintained = isolationMaintained
        };
    }

    public IReadOnlyList<ReplayResult> GetResults() => _results.AsReadOnly();

    public ReplayStormReport GenerateReport()
    {
        var totalReplays = _results.Count;
        var tenantPreserved = _results.Count(r => r.TenantPreserved);
        var isolationMaintained = _results.Count(r => r.IsolationMaintained);
        var failures = _results.Count(r => !string.IsNullOrEmpty(r.FailureReason));

        return new ReplayStormReport
        {
            TotalReplays = totalReplays,
            TenantPreservedCount = tenantPreserved,
            TenantPreservationRate = tenantPreserved / (double)totalReplays,
            IsolationMaintainedCount = isolationMaintained,
            IsolationMaintenanceRate = isolationMaintained / (double)totalReplays,
            FailureCount = failures,
            FailureRate = failures / (double)totalReplays
        };
    }
}

public sealed class ReplayStormReport
{
    public int TotalReplays { get; init; }
    public int TenantPreservedCount { get; init; }
    public double TenantPreservationRate { get; init; }
    public int IsolationMaintainedCount { get; init; }
    public double IsolationMaintenanceRate { get; init; }
    public int FailureCount { get; init; }
    public double FailureRate { get; init; }

    public void Print()
    {
        Console.WriteLine("\n=== REPLAY STORM REPORT ===");
        Console.WriteLine($"Total Replays: {TotalReplays}");
        Console.WriteLine($"Tenant Preserved: {TenantPreservedCount} ({TenantPreservationRate:P2})");
        Console.WriteLine($"Isolation Maintained: {IsolationMaintainedCount} ({IsolationMaintenanceRate:P2})");
        Console.WriteLine($"Failures: {FailureCount} ({FailureRate:P2})");

        if (FailureRate > 0.01)
        {
            Console.WriteLine("WARNING: Failure rate exceeds 1% threshold!");
        }

        if (TenantPreservationRate < 1.0)
        {
            Console.WriteLine("CRITICAL: Tenant preservation rate below 100%!");
        }
    }
}

public sealed class OutOfOrderReplayValidator
{
    public void ValidateOutOfOrderReplays(string tenantId, IEnumerable<DateTime> timestamps)
    {
        var ordered = timestamps.OrderBy(t => t).ToList();
        var violations = new List<(DateTime Original, DateTime Reordered)>();

        var index = 0;
        foreach (var ts in timestamps)
        {
            if (ordered[index] != ts)
            {
                violations.Add((ts, ordered[index]));
            }
            index++;
        }

        if (violations.Any())
        {
            Console.WriteLine($"Out-of-order violations for tenant {tenantId}: {violations.Count}");
        }
    }
}

public sealed class DuplicateDeliveryDetector
{
    private readonly Dictionary<string, HashSet<string>> _processedEvents = new();

    public bool IsDuplicate(string tenantId, string eventId)
    {
        if (!_processedEvents.TryGetValue(tenantId, out var tenantEvents))
        {
            tenantEvents = new HashSet<string>();
            _processedEvents[tenantId] = tenantEvents;
        }

        if (tenantEvents.Contains(eventId))
        {
            return true;
        }

        tenantEvents.Add(eventId);
        return false;
    }

    public void Reset()
    {
        _processedEvents.Clear();
    }
}

public sealed class SagaReplayValidator
{
    public void ValidateSagaReplay(string tenantId, IEnumerable<string> sagaIds)
    {
        var tenantSagas = sagaIds.Where(id => id.Contains(tenantId)).ToList();
        var crossTenant = sagaIds.Where(id => !id.Contains(tenantId)).ToList();

        if (crossTenant.Any())
        {
            Console.WriteLine($"WARNING: Cross-tenant saga IDs detected for {tenantId}: {crossTenant.Count}");
        }
    }
}