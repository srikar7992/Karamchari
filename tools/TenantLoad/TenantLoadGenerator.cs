namespace TenantLoad;

public sealed class TenantLoadConfig
{
    public required string[] TenantIds { get; init; }
    public int RequestsPerTenant { get; init; } = 100;
    public int ConcurrentTenants { get; init; } = 10;
    public int BurstSize { get; init; } = 50;
    public TimeSpan TestDuration { get; init; } = TimeSpan.FromMinutes(5);
    public LoadPattern Pattern { get; init; } = LoadPattern.Sustained;
}

public enum LoadPattern
{
    Sustained,
    Burst,
    Spike,
    Soak,
    Variable
}

public sealed class LoadMetrics
{
    public required string TenantId { get; init; }
    public int TotalRequests { get; init; }
    public int SuccessfulRequests { get; init; }
    public int FailedRequests { get; init; }
    public double AverageLatencyMs { get; init; }
    public double P50LatencyMs { get; init; }
    public double P95LatencyMs { get; init; }
    public double P99LatencyMs { get; init; }
    public double MaxLatencyMs { get; init; }
    public double ThroughputRps { get; init; }
    public double ErrorRate { get; init; }
    public bool IsolationMaintained { get; init; }
}

public sealed class TenantLoadGenerator
{
    private readonly TenantLoadConfig _config;
    private readonly List<LoadMetrics> _metrics = new();
    private bool _isRunning;

    public TenantLoadGenerator(TenantLoadConfig config)
    {
        _config = config;
    }

    public async Task RunLoadTestAsync(CancellationToken ct = default)
    {
        _isRunning = true;
        var startTime = DateTime.UtcNow;
        Console.WriteLine($"Starting load test: {_config.TenantIds.Length} tenants, {_config.RequestsPerTenant} requests each");

        var tasks = _config.TenantIds.Select(tenant => Task.Run(async () =>
        {
            var tenantMetrics = await SimulateTenantLoadAsync(tenant, ct);
            lock (_metrics)
            {
                _metrics.Add(tenantMetrics);
            }
        }));

        await Task.WhenAll(tasks);

        _isRunning = false;
        var duration = DateTime.UtcNow - startTime;
        Console.WriteLine($"Load test completed in {duration.TotalSeconds:F1}s");

        LogAggregateMetrics();
    }

    private async Task<LoadMetrics> SimulateTenantLoadAsync(string tenantId, CancellationToken ct)
    {
        var requestLatencies = new List<double>();
        var successCount = 0;
        var failCount = 0;

        for (int i = 0; i < _config.RequestsPerTenant && !ct.IsCancellationRequested; i++)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var success = SimulateRequest(tenantId, i);
            sw.Stop();

            requestLatencies.Add(sw.Elapsed.TotalMilliseconds);
            if (success) successCount++;
            else failCount++;

            await Task.Delay(Random.Shared.Next(0, 10), ct);
        }

        return new LoadMetrics
        {
            TenantId = tenantId,
            TotalRequests = _config.RequestsPerTenant,
            SuccessfulRequests = successCount,
            FailedRequests = failCount,
            AverageLatencyMs = requestLatencies.Average(),
            P50LatencyMs = Percentile(requestLatencies, 0.50),
            P95LatencyMs = Percentile(requestLatencies, 0.95),
            P99LatencyMs = Percentile(requestLatencies, 0.99),
            MaxLatencyMs = requestLatencies.Max(),
            ThroughputRps = successCount / (_config.RequestsPerTenant * 0.1),
            ErrorRate = failCount / (double)_config.RequestsPerTenant,
            IsolationMaintained = failCount < _config.RequestsPerTenant * 0.01
        };
    }

    private bool SimulateRequest(string tenantId, int requestId)
    {
        var simulatedLatency = Random.Shared.NextDouble() * 100;
        Thread.SpinWait(Random.Shared.Next(1000, 5000));

        var shouldSucceed = Random.Shared.NextDouble() > 0.001;
        return shouldSucceed;
    }

    private static double Percentile(List<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0) return 0;
        var sorted = sortedValues.OrderBy(v => v).ToList();
        var index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
        return sorted[Math.Max(0, index)];
    }

    private void LogAggregateMetrics()
    {
        var totalRequests = _metrics.Sum(m => m.TotalRequests);
        var totalSuccess = _metrics.Sum(m => m.SuccessfulRequests);
        var totalFail = _metrics.Sum(m => m.FailedRequests);
        var avgLatency = _metrics.Average(m => m.AverageLatencyMs);
        var maxP99 = _metrics.Max(m => m.P99LatencyMs);
        var isolationViolations = _metrics.Count(m => !m.IsolationMaintained);

        Console.WriteLine("\n=== AGGREGATE LOAD TEST RESULTS ===");
        Console.WriteLine($"Total Requests: {totalRequests:N0}");
        Console.WriteLine($"Successful: {totalSuccess:N0} ({(totalSuccess / (double)totalRequests):P2})");
        Console.WriteLine($"Failed: {totalFail:N0} ({(totalFail / (double)totalRequests):P2})");
        Console.WriteLine($"Average Latency: {avgLatency:F2}ms");
        Console.WriteLine($"Max P99 Latency: {maxP99:F2}ms");
        Console.WriteLine($"Isolation Violations: {isolationViolations}");

        if (isolationViolations > 0)
        {
            Console.WriteLine("WARNING: Isolation violations detected!");
        }
    }

    public IReadOnlyList<LoadMetrics> GetMetrics() => _metrics.AsReadOnly();
}

public sealed class NoisyNeighborSimulation
{
    private readonly List<NoiseProfile> _noiseProfiles = new();

    public void AddNoisyTenant(string tenantId, double noiseLevel, int burstFrequency)
    {
        _noiseProfiles.Add(new NoiseProfile
        {
            TenantId = tenantId,
            NoiseLevel = noiseLevel,
            BurstFrequency = burstFrequency
        });
    }

    public async Task SimulateNoisyNeighborAsync(string targetTenantId, TimeSpan duration, CancellationToken ct = default)
    {
        Console.WriteLine($"Simulating noisy neighbor affecting {targetTenantId}");
        var endTime = DateTime.UtcNow.Add(duration);

        while (DateTime.UtcNow < endTime && !ct.IsCancellationRequested)
        {
            foreach (var profile in _noiseProfiles)
            {
                if (profile.TenantId == targetTenantId) continue;

                var noiseLevel = profile.NoiseLevel;
                var requestCount = (int)(noiseLevel * 100);

                for (int i = 0; i < requestCount; i++)
                {
                    Thread.SpinWait(Random.Shared.Next(100, 1000));
                }
            }

            await Task.Delay(1000, ct);
        }
    }
}

public sealed class NoiseProfile
{
    public required string TenantId { get; init; }
    public double NoiseLevel { get; init; }
    public int BurstFrequency { get; init; }
}

public sealed class TenantStarvationDetector
{
    private readonly Dictionary<string, int> _requestCounts = new();
    private readonly object _lock = new();

    public void RecordRequest(string tenantId)
    {
        lock (_lock)
        {
            _requestCounts.TryGetValue(tenantId, out var count);
            _requestCounts[tenantId] = count + 1;
        }
    }

    public bool IsStarved(string tenantId, int expectedRequests, TimeSpan elapsed, TimeSpan expectedDuration)
    {
        lock (_lock)
        {
            if (!_requestCounts.TryGetValue(tenantId, out var actualRequests))
                return true;

            var expectedAtThisPoint = (int)(expectedRequests * (elapsed.TotalSeconds / expectedDuration.TotalSeconds));
            var threshold = expectedAtThisPoint * 0.5;

            return actualRequests < threshold;
        }
    }

    public Dictionary<string, int> GetRequestCounts()
    {
        lock (_lock)
        {
            return new Dictionary<string, int>(_requestCounts);
        }
    }
}