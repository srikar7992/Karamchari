namespace TenantChaos;

/// <summary>
/// Defines a specific chaos scenario to be injected into the tenant runtime.
/// </summary>
public sealed class TenantChaosScenario
{
    /// <summary>Gets the unique name of the chaos scenario.</summary>
    public required string Name { get; init; }
    
    /// <summary>Gets the category of chaos (e.g., Latency, ConnectionFailure).</summary>
    public required ChaosCategory Category { get; init; }
    
    /// <summary>Gets the severity level of the injected chaos.</summary>
    public required ChaosSeverity Severity { get; init; }
    
    /// <summary>Gets a description of the chaos being injected.</summary>
    public required string Description { get; init; }
    
    /// <summary>Gets how long the chaos should persist.</summary>
    public required TimeSpan Duration { get; init; }
    
    /// <summary>Gets specific parameters for the chaos injection.</summary>
    public Dictionary<string, string> Parameters { get; init; } = new();
    
    /// <summary>Gets the targets affected by this chaos scenario.</summary>
    public List<ChaosTarget> Targets { get; init; } = new();
}

/// <summary>
/// Categories of chaos that can be injected.
/// </summary>
public enum ChaosCategory
{
    /// <summary>Inject random network or processing latency.</summary>
    Latency,
    /// <summary>Inject database or network connection failures.</summary>
    ConnectionFailure,
    /// <summary>Inject malicious or corrupt data into cache.</summary>
    CachePoisoning,
    /// <summary>Simulate loss of TenantContext in an async flow.</summary>
    TenantContextLoss,
    /// <summary>Simulate a storm of message retries.</summary>
    RetryStorm,
    /// <summary>Simulate failure during database migration.</summary>
    MigrationInterruption,
    /// <summary>Simulate race conditions during schema provisioning.</summary>
    SchemaRace,
    /// <summary>Attempt to bypass Row Level Security via chaos.</summary>
    RlsBypass,
    /// <summary>Simulate connection pool contamination scenarios.</summary>
    ConnectionPoolContamination,
    /// <summary>Simulate tenant context drift in background jobs.</summary>
    BackgroundDrift
}

/// <summary>
/// Severity levels for chaos injection.
/// </summary>
public enum ChaosSeverity
{
    /// <summary>Minimal impact.</summary>
    Low,
    /// <summary>Noticeable performance degradation.</summary>
    Medium,
    /// <summary>Service instability for affected tenants.</summary>
    High,
    /// <summary>Complete service failure or isolation breach.</summary>
    Critical
}

/// <summary>
/// Represents a specific component or resource targeted by chaos injection.
/// </summary>
public sealed class ChaosTarget
{
    /// <summary>Gets the type of target (e.g., Database, Cache, Tenant).</summary>
    public required string TargetType { get; init; }
    
    /// <summary>Gets the specific identifier of the target.</summary>
    public required string TargetId { get; init; }
    
    /// <summary>Gets the list of tenants affected by targeting this resource.</summary>
    public required string[] AffectedTenants { get; init; }
}

/// <summary>
/// Component responsible for injecting various types of chaos into the tenant runtime
/// to verify isolation and resilience guarantees.
/// </summary>
public sealed class TenantChaosInjector
{
    private readonly List<TenantChaosScenario> _activeScenarios = new();
    private bool _isRunning;

    /// <summary>
    /// Injects random latency into operations for a specific tenant.
    /// </summary>
    public void InjectLatency(string tenantId, int minMs, int maxMs, TimeSpan duration)
    {
        var scenario = new TenantChaosScenario
        {
            Name = $"LatencyInjection_{tenantId}_{Guid.NewGuid():N}",
            Category = ChaosCategory.Latency,
            Severity = ChaosSeverity.Medium,
            Description = $"Inject random latency {minMs}-{maxMs}ms for tenant {tenantId}",
            Duration = duration,
            Parameters = new Dictionary<string, string>
            {
                ["minMs"] = minMs.ToString(),
                ["maxMs"] = maxMs.ToString(),
                ["tenantId"] = tenantId
            },
            Targets = new List<ChaosTarget>
            {
                new() { TargetType = "Tenant", TargetId = tenantId, AffectedTenants = new[] { tenantId } }
            }
        };

        _activeScenarios.Add(scenario);
        LogChaosEvent("LATENCY_INJECTED", scenario);
    }

    /// <summary>
    /// Injects database connection failures for a specific tenant.
    /// </summary>
    public void InjectConnectionFailure(string tenantId, double failureRate, TimeSpan duration)
    {
        var scenario = new TenantChaosScenario
        {
            Name = $"ConnectionFailure_{tenantId}_{Guid.NewGuid():N}",
            Category = ChaosCategory.ConnectionFailure,
            Severity = ChaosSeverity.Critical,
            Description = $"Inject {failureRate:P0} connection failures for tenant {tenantId}",
            Duration = duration,
            Parameters = new Dictionary<string, string>
            {
                ["failureRate"] = failureRate.ToString("P2"),
                ["tenantId"] = tenantId
            },
            Targets = new List<ChaosTarget>
            {
                new() { TargetType = "Database", TargetId = $"tenant_{tenantId}", AffectedTenants = new[] { tenantId } }
            }
        };

        _activeScenarios.Add(scenario);
        LogChaosEvent("CONNECTION_FAILURE_INJECTED", scenario);
    }

    /// <summary>
    /// Injects corrupt data into the cache for a specific tenant.
    /// </summary>
    public void InjectCachePoisoning(string tenantId, string cacheKey, string maliciousValue, TimeSpan duration)
    {
        var scenario = new TenantChaosScenario
        {
            Name = $"CachePoisoning_{tenantId}_{Guid.NewGuid():N}",
            Category = ChaosCategory.CachePoisoning,
            Severity = ChaosSeverity.High,
            Description = $"Poison cache key {cacheKey} with malicious value for tenant {tenantId}",
            Duration = duration,
            Parameters = new Dictionary<string, string>
            {
                ["cacheKey"] = cacheKey,
                ["maliciousValue"] = maliciousValue,
                ["tenantId"] = tenantId
            },
            Targets = new List<ChaosTarget>
            {
                new() { TargetType = "Cache", TargetId = cacheKey, AffectedTenants = new[] { tenantId } }
            }
        };

        _activeScenarios.Add(scenario);
        LogChaosEvent("CACHE_POISONING_INJECTED", scenario);
    }

    /// <summary>
    /// Simulates sudden loss of tenant execution context during an async operation.
    /// </summary>
    public void SimulateTenantContextLoss(string tenantId, TimeSpan duration)
    {
        var scenario = new TenantChaosScenario
        {
            Name = $"TenantContextLoss_{tenantId}_{Guid.NewGuid():N}",
            Category = ChaosCategory.TenantContextLoss,
            Severity = ChaosSeverity.Critical,
            Description = $"Simulate tenant context loss for {tenantId}",
            Duration = duration,
            Parameters = new Dictionary<string, string>
            {
                ["tenantId"] = tenantId,
                ["lossDuration"] = duration.TotalSeconds.ToString("F1")
            },
            Targets = new List<ChaosTarget>
            {
                new() { TargetType = "AsyncLocal", TargetId = "TenantContext", AffectedTenants = new[] { tenantId } }
            }
        };

        _activeScenarios.Add(scenario);
        LogChaosEvent("TENANT_CONTEXT_LOSS_INJECTED", scenario);
    }

    /// <summary>
    /// Simulates a retry storm for a specific tenant's messages.
    /// </summary>
    public void SimulateRetryStorm(string tenantId, int stormIntensity, TimeSpan duration)
    {
        var scenario = new TenantChaosScenario
        {
            Name = $"RetryStorm_{tenantId}_{Guid.NewGuid():N}",
            Category = ChaosCategory.RetryStorm,
            Severity = ChaosSeverity.Critical,
            Description = $"Simulate retry storm with intensity {stormIntensity} for tenant {tenantId}",
            Duration = duration,
            Parameters = new Dictionary<string, string>
            {
                ["stormIntensity"] = stormIntensity.ToString(),
                ["tenantId"] = tenantId
            },
            Targets = new List<ChaosTarget>
            {
                new() { TargetType = "RetryQueue", TargetId = $"retry_{tenantId}", AffectedTenants = new[] { tenantId } }
            }
        };

        _activeScenarios.Add(scenario);
        LogChaosEvent("RETRY_STORM_INJECTED", scenario);
    }

    /// <summary>
    /// Simulates race conditions during schema provisioning.
    /// </summary>
    public void SimulateSchemaRace(string tenantId, int raceParticipantCount, TimeSpan duration)
    {
        var scenario = new TenantChaosScenario
        {
            Name = $"SchemaRace_{tenantId}_{Guid.NewGuid():N}",
            Category = ChaosCategory.SchemaRace,
            Severity = ChaosSeverity.High,
            Description = $"Simulate schema provisioning race with {raceParticipantCount} participants for tenant {tenantId}",
            Duration = duration,
            Parameters = new Dictionary<string, string>
            {
                ["raceParticipantCount"] = raceParticipantCount.ToString(),
                ["tenantId"] = tenantId
            },
            Targets = new List<ChaosTarget>
            {
                new() { TargetType = "Schema", TargetId = $"tenant_{tenantId}", AffectedTenants = new[] { tenantId } }
            }
        };

        _activeScenarios.Add(scenario);
        LogChaosEvent("SCHEMA_RACE_INJECTED", scenario);
    }

    /// <summary>
    /// Simulates connection pool contamination across multiple tenants.
    /// </summary>
    public void SimulateConnectionPoolContamination(string[] tenantIds, TimeSpan duration)
    {
        var scenario = new TenantChaosScenario
        {
            Name = $"PoolContamination_{Guid.NewGuid():N}",
            Category = ChaosCategory.ConnectionPoolContamination,
            Severity = ChaosSeverity.Critical,
            Description = $"Simulate connection pool contamination across {tenantIds.Length} tenants",
            Duration = duration,
            Parameters = new Dictionary<string, string>
            {
                ["tenantIds"] = string.Join(",", tenantIds),
                ["affectedCount"] = tenantIds.Length.ToString()
            },
            Targets = tenantIds.Select(t => new ChaosTarget
            {
                TargetType = "ConnectionPool",
                TargetId = $"pool_{t}",
                AffectedTenants = tenantIds
            }).ToList()
        };

        _activeScenarios.Add(scenario);
        LogChaosEvent("POOL_CONTAMINATION_INJECTED", scenario);
    }

    /// <summary>
    /// Simulates tenant context drift in background job processing.
    /// </summary>
    public void SimulateBackgroundDrift(string tenantId, TimeSpan duration)
    {
        var scenario = new TenantChaosScenario
        {
            Name = $"BackgroundDrift_{tenantId}_{Guid.NewGuid():N}",
            Category = ChaosCategory.BackgroundDrift,
            Severity = ChaosSeverity.Critical,
            Description = $"Simulate background job tenant context drift for {tenantId}",
            Duration = duration,
            Parameters = new Dictionary<string, string>
            {
                ["tenantId"] = tenantId,
                ["driftDuration"] = duration.TotalSeconds.ToString("F1")
            },
            Targets = new List<ChaosTarget>
            {
                new() { TargetType = "BackgroundJob", TargetId = $"job_{tenantId}", AffectedTenants = new[] { tenantId } }
            }
        };

        _activeScenarios.Add(scenario);
        LogChaosEvent("BACKGROUND_DRIFT_INJECTED", scenario);
    }

    /// <summary>
    /// Attempts to bypass RLS via chaos manipulation.
    /// </summary>
    public void InjectRlsBypass(string tenantId, string bypassType, TimeSpan duration)
    {
        var scenario = new TenantChaosScenario
        {
            Name = $"RlsBypass_{tenantId}_{Guid.NewGuid():N}",
            Category = ChaosCategory.RlsBypass,
            Severity = ChaosSeverity.Critical,
            Description = $"Attempt RLS bypass of type {bypassType} for tenant {tenantId}",
            Duration = duration,
            Parameters = new Dictionary<string, string>
            {
                ["bypassType"] = bypassType,
                ["tenantId"] = tenantId
            },
            Targets = new List<ChaosTarget>
            {
                new() { TargetType = "RLS", TargetId = $"tenant_{tenantId}", AffectedTenants = new[] { tenantId } }
            }
        };

        _activeScenarios.Add(scenario);
        LogChaosEvent("RLS_BYPASS_ATTEMPTED", scenario);
    }

    /// <summary>
    /// Asynchronously runs a specific chaos scenario.
    /// </summary>
    public async Task RunChaosScenarioAsync(TenantChaosScenario scenario, CancellationToken ct = default)
    {
        _isRunning = true;
        LogChaosEvent("CHAOS_SCENARIO_STARTED", scenario);

        var endTime = DateTime.UtcNow.Add(scenario.Duration);
        while (DateTime.UtcNow < endTime && !ct.IsCancellationRequested)
        {
            await Task.Delay(1000, ct);
            LogChaosEvent("CHAOS_PULSE", scenario);
        }

        StopChaosScenario(scenario.Name);
    }

    /// <summary>
    /// Stops a specific active chaos scenario by name.
    /// </summary>
    public void StopChaosScenario(string scenarioName)
    {
        var scenario = _activeScenarios.FirstOrDefault(s => s.Name == scenarioName);
        if (scenario != null)
        {
            _activeScenarios.Remove(scenario);
            LogChaosEvent("CHAOS_SCENARIO_STOPPED", scenario);
        }
    }

    /// <summary>
    /// Stops all currently active chaos scenarios.
    /// </summary>
    public void StopAllChaos()
    {
        _isRunning = false;
        foreach (var scenario in _activeScenarios.ToList())
        {
            StopChaosScenario(scenario.Name);
        }
        _activeScenarios.Clear();
        LogChaosEvent("ALL_CHAOS_STOPPED", null!);
    }

    /// <summary>
    /// Gets the collection of currently active chaos scenarios.
    /// </summary>
    public IReadOnlyList<TenantChaosScenario> GetActiveScenarios() => _activeScenarios.AsReadOnly();

    private void LogChaosEvent(string eventType, TenantChaosScenario? scenario)
    {
        var timestamp = DateTime.UtcNow.ToString("O");
        var targetInfo = (scenario?.Targets != null && scenario.Targets.Any())
            ? string.Join(",", scenario.Targets.Select(t => $"{t.TargetType}:{t.TargetId}"))
            : "N/A";

        Console.WriteLine($"[{timestamp}] CHAOS_EVENT [{eventType}] " +
            $"Scenario={scenario?.Name ?? "N/A"} " +
            $"Category={scenario?.Category.ToString() ?? "N/A"} " +
            $"Severity={scenario?.Severity.ToString() ?? "N/A"} " +
            $"Targets={targetInfo}");
    }
}

/// <summary>
/// Comprehensive report detailing the execution and impact of a chaos scenario.
/// </summary>
public sealed class ChaosReport
{
    /// <summary>Gets the name of the scenario.</summary>
    public required string ScenarioName { get; init; }
    
    /// <summary>Gets the actual start time.</summary>
    public DateTime StartTime { get; init; }
    
    /// <summary>Gets the actual end time.</summary>
    public DateTime EndTime { get; init; }
    
    /// <summary>Gets the actual duration.</summary>
    public TimeSpan Duration => EndTime - StartTime;
    
    /// <summary>Gets the category of chaos.</summary>
    public ChaosCategory Category { get; init; }
    
    /// <summary>Gets the severity level.</summary>
    public ChaosSeverity Severity { get; init; }
    
    /// <summary>Gets the collection of pulses recorded during the scenario.</summary>
    public List<ChaosPulse> Pulses { get; init; } = new();
    
    /// <summary>Gets whether the scenario completed successfully.</summary>
    public bool Completed { get; init; }
    
    /// <summary>Gets whether the scenario failed unexpectedly.</summary>
    public bool Failed { get; init; }
    
    /// <summary>Gets the failure reason, if any.</summary>
    public string? FailureReason { get; init; }
}

/// <summary>
/// A single diagnostic point recorded during a chaos scenario.
/// </summary>
public sealed class ChaosPulse
{
    /// <summary>Gets the pulse timestamp.</summary>
    public DateTime Timestamp { get; init; }
    
    /// <summary>Gets the sequence count.</summary>
    public int PulseCount { get; init; }
    
    /// <summary>Gets metrics captured at the time of the pulse.</summary>
    public Dictionary<string, string> Metrics { get; init; } = new();
}

/// <summary>
/// Orchestrates complex chaos campaigns involving multiple scenarios and tenants.
/// </summary>
public sealed class TenantChaosOrchestrator
{
    private readonly TenantChaosInjector _injector = new();
    private readonly List<ChaosReport> _reports = new();

    /// <summary>
    /// Asynchronously executes a defined chaos campaign.
    /// </summary>
    public async Task RunChaosCampaignAsync(ChaosCampaignConfig config, CancellationToken ct = default)
    {
        Console.WriteLine($"Starting Chaos Campaign: {config.Name}");
        Console.WriteLine($"Duration: {config.Duration}");
        Console.WriteLine($"Intensity: {config.Intensity}");

        var startTime = DateTime.UtcNow;
        var endTime = startTime.Add(config.Duration);

        while (DateTime.UtcNow < endTime && !ct.IsCancellationRequested)
        {
            foreach (var scenarioConfig in config.Scenarios)
            {
                if (DateTime.UtcNow >= endTime) break;

                _injector.InjectLatency(
                    scenarioConfig.TenantId,
                    scenarioConfig.MinLatencyMs,
                    scenarioConfig.MaxLatencyMs,
                    TimeSpan.FromSeconds(10));
            }

            await Task.Delay(TimeSpan.FromSeconds(config.Intensity), ct);
        }

        _injector.StopAllChaos();

        var report = new ChaosReport
        {
            ScenarioName = config.Name,
            StartTime = startTime,
            EndTime = DateTime.UtcNow,
            Category = ChaosCategory.Latency,
            Severity = ChaosSeverity.Medium,
            Completed = !ct.IsCancellationRequested,
            Failed = ct.IsCancellationRequested
        };

        _reports.Add(report);
    }

    /// <summary>
    /// Gets all reports from previously executed campaigns.
    /// </summary>
    public IReadOnlyList<ChaosReport> GetReports() => _reports.AsReadOnly();
}

/// <summary>
/// Configuration for a chaos campaign.
/// </summary>
public sealed class ChaosCampaignConfig
{
    /// <summary>Gets the unique name of the campaign.</summary>
    public required string Name { get; init; }
    
    /// <summary>Gets the total duration of the campaign.</summary>
    public required TimeSpan Duration { get; init; }
    
    /// <summary>Gets the frequency/intensity of chaos pulses in seconds.</summary>
    public required int Intensity { get; init; }
    
    /// <summary>Gets the collection of scenario configurations.</summary>
    public List<ChaosScenarioConfig> Scenarios { get; init; } = new();
}

/// <summary>
/// Configuration for an individual chaos scenario within a campaign.
/// </summary>
public sealed class ChaosScenarioConfig
{
    /// <summary>Gets the target tenant ID.</summary>
    public required string TenantId { get; init; }
    
    /// <summary>Gets the category of chaos.</summary>
    public required ChaosCategory Category { get; init; }
    
    /// <summary>Gets the minimum latency to inject.</summary>
    public int MinLatencyMs { get; init; } = 100;
    
    /// <summary>Gets the maximum latency to inject.</summary>
    public int MaxLatencyMs { get; init; } = 500;
    
    /// <summary>Gets the injected failure rate.</summary>
    public double FailureRate { get; init; } = 0.1;
}
