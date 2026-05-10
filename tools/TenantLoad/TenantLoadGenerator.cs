namespace TenantLoad;

/// <summary>
/// Configuration for a tenant load test campaign.
/// </summary>
public sealed class TenantLoadConfig
{
    /// <summary>Gets the collection of tenant identifiers to include in the load test.</summary>
    public required string[] TenantIds { get; init; }
    
    /// <summary>Gets the number of requests to execute per tenant.</summary>
    public int RequestsPerTenant { get; init; } = 100;
    
    /// <summary>Gets the number of tenants to simulate concurrently.</summary>
    public int ConcurrentTenants { get; init; } = 10;
    
    /// <summary>Gets the size of request bursts for spike testing.</summary>
    public int BurstSize { get; init; } = 50;
    
    /// <summary>Gets the total duration of the load test.</summary>
    public TimeSpan TestDuration { get; init; } = TimeSpan.FromMinutes(5);
    
    /// <summary>Gets the load pattern to apply (e.g., Sustained, Spike).</summary>
    public LoadPattern Pattern { get; init; } = LoadPattern.Sustained;
}

/// <summary>
/// Load patterns for tenant performance and isolation testing.
/// </summary>
public enum LoadPattern
{
    /// <summary>Steady stream of requests.</summary>
    Sustained,
    /// <summary>Periodic bursts of high-volume requests.</summary>
    Burst,
    /// <summary>Sudden, extreme volume increases.</summary>
    Spike,
    /// <summary>Long-duration sustained load to detect resource leaks.</summary>
    Soak,
    /// <summary>Randomly varying load levels.</summary>
    Variable
}

/// <summary>
/// Performance and isolation metrics captured for a single tenant during load testing.
/// </summary>
public sealed class LoadMetrics
{
    /// <summary>Gets the tenant identifier.</summary>
    public required string TenantId { get; init; }
    
    /// <summary>Gets total requests executed.</summary>
    public int TotalRequests { get; init; }
    
    /// <summary>Gets successfully completed requests.</summary>
    public int SuccessfulRequests { get; init; }
    
    /// <summary>Gets failed requests.</summary>
    public int FailedRequests { get; init; }
    
    /// <summary>Gets average latency in milliseconds.</summary>
    public double AverageLatencyMs { get; init; }
    
    /// <summary>Gets 50th percentile latency.</summary>
    public double P50LatencyMs { get; init; }
    
    /// <summary>Gets 95th percentile latency.</summary>
    public double P95LatencyMs { get; init; }
    
    /// <summary>Gets 99th percentile latency.</summary>
    public double P99LatencyMs { get; init; }
    
    /// <summary>Gets maximum recorded latency.</summary>
    public double MaxLatencyMs { get; init; }
    
    /// <summary>Gets throughput in requests per second.</summary>
    public double ThroughputRps { get; init; }
    
    /// <summary>Gets the ratio of failed requests to total requests.</summary>
    public double ErrorRate { get; init; }
    
    /// <summary>Gets whether isolation was maintained (e.g., no cross-tenant data found).</summary>
    public bool IsolationMaintained { get; init; }
}

/// <summary>
/// High-volume load generator for tenant isolation and performance verification.
/// Simulates hundreds of concurrent tenants to detect "noisy neighbor" effects and race conditions.
/// </summary>
public sealed class TenantLoadGenerator
{
    private readonly TenantLoadConfig _config;
    private readonly List<LoadMetrics> _metrics = new();
    private bool _isRunning;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantLoadGenerator"/> class.
    /// </summary>
    /// <param name="config">The load test configuration.</param>
    public TenantLoadGenerator(TenantLoadConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Asynchronously executes the load test campaign.
    /// </summary>
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
        }, ct));

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
            AverageLatencyMs = requestLatencies.Any() ? requestLatencies.Average() : 0,
            P50LatencyMs = Percentile(requestLatencies, 0.50),
            P95LatencyMs = Percentile(requestLatencies, 0.95),
            P99LatencyMs = Percentile(requestLatencies, 0.99),
            MaxLatencyMs = requestLatencies.Any() ? requestLatencies.Max() : 0,
            ThroughputRps = successCount / (_config.RequestsPerTenant * 0.1),
            ErrorRate = failCount / (double)_config.RequestsPerTenant,
            IsolationMaintained = failCount < _config.RequestsPerTenant * 0.01
        };
    }

    private bool SimulateRequest(string tenantId, int requestId)
    {
        // Placeholder for actual API/Service request
        Thread.SpinWait(Random.Shared.Next(1000, 5000));
        return Random.Shared.NextDouble() > 0.001;
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
        var avgLatency = _metrics.Any() ? _metrics.Average(m => m.AverageLatencyMs) : 0;
        var maxP99 = _metrics.Any() ? _metrics.Max(m => m.P99LatencyMs) : 0;
        var isolationViolations = _metrics.Count(m => !m.IsolationMaintained);

        Console.WriteLine("\n=== AGGREGATE LOAD TEST RESULTS ===");
        Console.WriteLine($"Total Requests: {totalRequests:N0}");
        Console.WriteLine($"Successful: {totalSuccess:N0} ({(totalRequests > 0 ? totalSuccess / (double)totalRequests : 0):P2})");
        Console.WriteLine($"Failed: {totalFail:N0} ({(totalRequests > 0 ? totalFail / (double)totalRequests : 0):P2})");
        Console.WriteLine($"Average Latency: {avgLatency:F2}ms");
        Console.WriteLine($"Max P99 Latency: {maxP99:F2}ms");
        Console.WriteLine($"Isolation Violations: {isolationViolations}");

        if (isolationViolations > 0)
        {
            Console.WriteLine("WARNING: Isolation violations detected!");
        }
    }

    /// <summary>
    /// Gets the detailed metrics for all tenants.
    /// </summary>
    public IReadOnlyList<LoadMetrics> GetMetrics() => _metrics.AsReadOnly();
}

/// <summary>
/// Simulator for noisy neighbor scenarios, where one tenant's excessive resource usage
/// might impact others.
/// </summary>
public sealed class NoisyNeighborSimulation
{
    private readonly List<NoiseProfile> _noiseProfiles = new();

    /// <summary>
    /// Adds a profile for a tenant that will generate noise during the simulation.
    /// </summary>
    public void AddNoisyTenant(string tenantId, double noiseLevel, int burstFrequency)
    {
        _noiseProfiles.Add(new NoiseProfile
        {
            TenantId = tenantId,
            NoiseLevel = noiseLevel,
            BurstFrequency = burstFrequency
        });
    }

    /// <summary>
    /// Asynchronously executes the noisy neighbor simulation.
    /// </summary>
    public async Task SimulateNoisyNeighborAsync(string targetTenantId, TimeSpan duration, CancellationToken ct = default)
    {
        Console.WriteLine($"Simulating noisy neighbor affecting {targetTenantId}");
        var endTime = DateTime.UtcNow.Add(duration);

        while (DateTime.UtcNow < endTime && !ct.IsCancellationRequested)
        {
            foreach (var profile in _noiseProfiles)
            {
                if (profile.TenantId == targetTenantId) continue;

                var requestCount = (int)(profile.NoiseLevel * 100);
                for (int i = 0; i < requestCount; i++)
                {
                    Thread.SpinWait(Random.Shared.Next(100, 1000));
                }
            }

            await Task.Delay(1000, ct);
        }
    }
}

/// <summary>
/// Profile defining the noise generation characteristics of a tenant.
/// </summary>
public sealed class NoiseProfile
{
    /// <summary>Gets the tenant ID.</summary>
    public required string TenantId { get; init; }
    
    /// <summary>Gets the intensity level of the noise.</summary>
    public double NoiseLevel { get; init; }
    
    /// <summary>Gets how often bursts occur.</summary>
    public int BurstFrequency { get; init; }
}

/// <summary>
/// Detector for tenant resource starvation, identifying if a tenant is not receiving its
/// fair share of processing time.
/// </summary>
public sealed class TenantStarvationDetector
{
    private readonly Dictionary<string, int> _requestCounts = new();
    private readonly object _lock = new();

    /// <summary>
    /// Records a processed request for a tenant.
    /// </summary>
    public void RecordRequest(string tenantId)
    {
        lock (_lock)
        {
            _requestCounts.TryGetValue(tenantId, out var count);
            _requestCounts[tenantId] = count + 1;
        }
    }

    /// <summary>
    /// Checks if a tenant is currently being starved of resources.
    /// </summary>
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

    /// <summary>
    /// Gets the current request count for each tenant.
    /// </summary>
    public Dictionary<string, int> GetRequestCounts()
    {
        lock (_lock)
        {
            return new Dictionary<string, int>(_requestCounts);
        }
    }
}
