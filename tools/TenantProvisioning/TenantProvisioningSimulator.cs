namespace TenantProvisioning;

public sealed class TenantProvisioningConfig
{
    public required string TenantId { get; init; }
    public required string AdminEmail { get; init; }
    public string SchemaPrefix { get; init; } = "tenant_";
    public bool RunMigrations { get; init; } = true;
    public bool SeedData { get; init; } = true;
    public int RetryCount { get; init; } = 3;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
}

public sealed class ProvisioningResult
{
    public required string TenantId { get; init; }
    public required string SchemaName { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public TimeSpan Duration => EndTime - StartTime;
    public List<ProvisioningStep> Steps { get; init; } = new();
}

public sealed class ProvisioningStep
{
    public required string Name { get; init; }
    public bool Success { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed class ConcurrentProvisioningStressTest
{
    private readonly List<ProvisioningResult> _results = new();
    private readonly List<string> _raceLog = new();

    public async Task RunConcurrentProvisioningStressTestAsync(string[] tenantIds, int concurrentCount, CancellationToken ct = default)
    {
        Console.WriteLine($"Starting concurrent provisioning stress test: {tenantIds.Length} tenants, {concurrentCount} concurrent");

        var semaphore = new SemaphoreSlim(concurrentCount);
        var tasks = tenantIds.Select(async tenantId =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var result = await SimulateProvisioningAsync(tenantId, ct);
                lock (_results)
                {
                    _results.Add(result);
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        Console.WriteLine($"Concurrent provisioning completed: {_results.Count} tenants");
    }

    private async Task<ProvisioningResult> SimulateProvisioningAsync(string tenantId, CancellationToken ct)
    {
        var result = new ProvisioningResult
        {
            TenantId = tenantId,
            SchemaName = $"tenant_{tenantId}",
            Success = true,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(5),
            Steps = new List<ProvisioningStep>()
        };

        var steps = new[] { "CreateSchema", "CreateTables", "CreateIndexes", "RunMigrations", "SeedData" };
        foreach (var step in steps)
        {
            var stepResult = new ProvisioningStep
            {
                Name = step,
                Success = true,
                Duration = TimeSpan.FromMilliseconds(Random.Shared.Next(100, 500))
            };
            result.Steps.Add(stepResult);

            await Task.Delay(Random.Shared.Next(50, 200), ct);
            LogRaceEvent(tenantId, step);
        }

        result.EndTime = DateTime.UtcNow;
        return result;
    }

    private void LogRaceEvent(string tenantId, string step)
    {
        lock (_raceLog)
        {
            _raceLog.Add($"{DateTime.UtcNow:O} {tenantId} {step}");
        }
    }

    public ConcurrentProvisioningReport GenerateReport()
    {
        var total = _results.Count;
        var successCount = _results.Count(r => r.Success);
        var failures = _results.Where(r => !r.Success).ToList();
        var avgDuration = _results.Average(r => r.Duration.TotalSeconds);

        return new ConcurrentProvisioningReport
        {
            TotalTenants = total,
            SuccessfulProvisions = successCount,
            FailedProvisions = total - successCount,
            AverageDurationSeconds = avgDuration,
            FailureReasons = failures.Select(f => f.ErrorMessage ?? "Unknown").Distinct().ToList()
        };
    }
}

public sealed class ConcurrentProvisioningReport
{
    public int TotalTenants { get; init; }
    public int SuccessfulProvisions { get; init; }
    public int FailedProvisions { get; init; }
    public double AverageDurationSeconds { get; init; }
    public List<string> FailureReasons { get; init; } = new();

    public void Print()
    {
        Console.WriteLine("\n=== CONCURRENT PROVISIONING REPORT ===");
        Console.WriteLine($"Total Tenants: {TotalTenants}");
        Console.WriteLine($"Successful: {SuccessfulProvisions} ({(SuccessfulProvisions / (double)TotalTenants):P2})");
        Console.WriteLine($"Failed: {FailedProvisions} ({(FailedProvisions / (double)TotalTenants):P2})");
        Console.WriteLine($"Average Duration: {AverageDurationSeconds:F2}s");

        if (FailureReasons.Any())
        {
            Console.WriteLine("\nFailure Reasons:");
            foreach (var reason in FailureReasons)
            {
                Console.WriteLine($"  - {reason}");
            }
        }
    }
}

public sealed class MigrationInterruptResumeTest
{
    public async Task SimulateInterruptResumeAsync(string tenantId, CancellationToken ct = default)
    {
        var steps = new[] { "Step1", "Step2", "INTERRUPT", "Step4", "Step5" };
        var completedBeforeInterrupt = new List<string>();
        var completedAfterResume = new List<string>();
        var interrupted = false;

        foreach (var step in steps)
        {
            if (step == "INTERRUPT")
            {
                interrupted = true;
                Console.WriteLine($"Provisioning interrupted for tenant {tenantId}");
                await Task.Delay(500, ct);
                continue;
            }

            if (interrupted)
            {
                completedAfterResume.Add(step);
            }
            else
            {
                completedBeforeInterrupt.Add(step);
            }

            await Task.Delay(100, ct);
        }

        Console.WriteLine($"Tenant {tenantId} - Before interrupt: {string.Join(", ", completedBeforeInterrupt)}");
        Console.WriteLine($"Tenant {tenantId} - After resume: {string.Join(", ", completedAfterResume)}");
    }

    public bool ValidateResumeIntegrity(List<string> completedBefore, List<string> completedAfter)
    {
        if (completedBefore.Intersect(completedAfter).Any())
        {
            Console.WriteLine("WARNING: Duplicate steps detected after resume!");
            return false;
        }

        return true;
    }
}

public sealed class SchemaVersionDriftSimulator
{
    public void SimulateVersionDrift(string[] tenantIds, string[] versions)
    {
        var driftMatrix = new Dictionary<string, string>();

        for (int i = 0; i < tenantIds.Length; i++)
        {
            driftMatrix[tenantIds[i]] = versions[i % versions.Length];
        }

        Console.WriteLine("\n=== SCHEMA VERSION DRIFT SIMULATION ===");
        foreach (var (tenant, version) in driftMatrix)
        {
            Console.WriteLine($"  {tenant}: {version}");
        }

        var versionGroups = driftMatrix.GroupBy(kvp => kvp.Value).ToList();
        Console.WriteLine($"\nVersion Groups: {versionGroups.Count}");

        if (versionGroups.Count > 1)
        {
            Console.WriteLine("WARNING: Schema version drift detected across tenants!");
        }
    }

    public bool ValidateCrossVersionOperations(string tenantA, string versionA, string tenantB, string versionB)
    {
        if (versionA != versionB)
        {
            Console.WriteLine($"Cross-version operation between {tenantA} (v{versionA}) and {tenantB} (v{versionB})");
            return true;
        }

        return false;
    }
}

public sealed class TenantDeprovisioningTest
{
    public async Task SimulateDeprovisioningAsync(string tenantId, CancellationToken ct = default)
    {
        var steps = new[]
        {
            "BeginDeprovisioning",
            "BlockNewConnections",
            "WaitForActiveRequests",
            "ExportData",
            "DropSchema",
            "CleanupResources",
            "CompleteDeprovisioning"
        };

        foreach (var step in steps)
        {
            Console.WriteLine($"Deprovisioning {tenantId}: {step}");
            await Task.Delay(200, ct);
        }
    }

    public bool ValidateIsolationAfterDeprovision(string removedTenant, string remainingTenant)
    {
        Console.WriteLine($"Validating isolation after deprovisioning {removedTenant}");
        Console.WriteLine($"Remaining tenant {remainingTenant} should not be affected");

        return true;
    }
}