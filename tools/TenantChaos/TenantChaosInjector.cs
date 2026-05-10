namespace TenantChaos;

public sealed class TenantChaosScenario
{
    public required string Name { get; init; }
    public required ChaosCategory Category { get; init; }
    public required ChaosSeverity Severity { get; init; }
    public required string Description { get; init; }
    public required TimeSpan Duration { get; init; }
    public Dictionary<string, string> Parameters { get; init; } = new();
    public List<ChaosTarget> Targets { get; init; } = new();
}

public enum ChaosCategory
{
    Latency,
    ConnectionFailure,
    CachePoisoning,
    TenantContextLoss,
    RetryStorm,
    MigrationInterruption,
    SchemaRace,
    RlsBypass,
    ConnectionPoolContamination,
    BackgroundDrift
}

public enum ChaosSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public sealed class ChaosTarget
{
    public required string TargetType { get; init; }
    public required string TargetId { get; init; }
    public required string[] AffectedTenants { get; init; }
}

public sealed class TenantChaosInjector
{
    private readonly List<ChaosScenario> _activeScenarios = new();
    private readonly Random _random = new();
    private bool _isRunning;

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

    public async Task RunChaosScenarioAsync(ChaosScenario scenario, CancellationToken ct = default)
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

    public void StopChaosScenario(string scenarioName)
    {
        var scenario = _activeScenarios.FirstOrDefault(s => s.Name == scenarioName);
        if (scenario != null)
        {
            _activeScenarios.Remove(scenario);
            LogChaosEvent("CHAOS_SCENARIO_STOPPED", scenario);
        }
    }

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

    public IReadOnlyList<ChaosScenario> GetActiveScenarios() => _activeScenarios.AsReadOnly();

    private void LogChaosEvent(string eventType, TenantChaosScenario scenario)
    {
        var timestamp = DateTime.UtcNow.ToString("O");
        var targetInfo = scenario.Targets.Any()
            ? string.Join(",", scenario.Targets.Select(t => $"{t.TargetType}:{t.TargetId}"))
            : "N/A";

        Console.WriteLine($"[{timestamp}] CHAOS_EVENT [{eventType}] " +
            $"Scenario={scenario.Name} " +
            $"Category={scenario.Category} " +
            $"Severity={scenario.Severity} " +
            $"Targets={targetInfo}");
    }
}

public sealed class ChaosReport
{
    public required string ScenarioName { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public TimeSpan Duration => EndTime - StartTime;
    public ChaosCategory Category { get; init; }
    public ChaosSeverity Severity { get; init; }
    public List<ChaosPulse> Pulses { get; init; } = new();
    public bool Completed { get; init; }
    public bool Failed { get; init; }
    public string? FailureReason { get; init; }
}

public sealed class ChaosPulse
{
    public DateTime Timestamp { get; init; }
    public int PulseCount { get; init; }
    public Dictionary<string, string> Metrics { get; init; } = new();
}

public sealed class TenantChaosOrchestrator
{
    private readonly TenantChaosInjector _injector = new();
    private readonly List<ChaosReport> _reports = new();

    public async Task RunChaosCampaignAsync(ChaosCampaignConfig config, CancellationToken ct = default)
    {
        Console.WriteLine($"Starting Chaos Campaign: {config.Name}");
        Console.WriteLine($"Duration: {config.Duration}");
        Console.WriteLine($"Intensity: {config.Intensity}");

        var startTime = DateTime.UtcNow;
        var endTime = startTime.Add(config.Duration);
        var scenariosExecuted = 0;

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

                scenariosExecuted++;
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

    public IReadOnlyList<ChaosReport> GetReports() => _reports.AsReadOnly();
}

public sealed class ChaosCampaignConfig
{
    public required string Name { get; init; }
    public required TimeSpan Duration { get; init; }
    public required int Intensity { get; init; }
    public List<ChaosScenarioConfig> Scenarios { get; init; } = new();
}

public sealed class ChaosScenarioConfig
{
    public required string TenantId { get; init; }
    public required ChaosCategory Category { get; init; }
    public int MinLatencyMs { get; init; } = 100;
    public int MaxLatencyMs { get; init; } = 500;
    public double FailureRate { get; init; } = 0.1;
}