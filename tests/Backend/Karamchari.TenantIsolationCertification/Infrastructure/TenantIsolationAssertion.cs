using System.Collections.Concurrent;
using System.Diagnostics;
using FluentAssertions;

namespace Karamchari.TenantIsolationCertification.Infrastructure;

public sealed class TenantIsolationAssertion
{
    private readonly TenantTestContext _context;

    public TenantIsolationAssertion(TenantTestContext context)
    {
        _context = context;
    }

    public void AssertCurrentTenantIs(string expectedTenantId)
    {
        var normalized = expectedTenantId.ToLowerInvariant();
        _context.ActiveTenantId.Should().Be(normalized,
            $"Expected tenant '{normalized}' but got '{_context.ActiveTenantId}'. TENANT BLEED DETECTED!");
    }

    public void AssertNoTenantCrossContamination()
    {
        _context.ActiveTenantId.Should().NotBeEmpty(
            "Tenant context is EMPTY - potential tenant bleed to public/default scope");
    }

    public void AssertSchemaMatchesTenant()
    {
        var expectedSchema = $"tenant_{_context.ActiveTenantId}";
        _context.ActiveSchema.Should().Be(expectedSchema,
            $"Schema mismatch: expected '{expectedSchema}' but got '{_context.ActiveSchema}'. Schema isolation FAILURE!");
    }

    public void AssertTenantIdFormatValid()
    {
        System.Text.RegularExpressions.Regex.IsMatch(
            _context.ActiveTenantId,
            "^[a-z0-9][a-z0-9_-]{0,49}$"
        ).Should().BeTrue(
            $"Tenant ID '{_context.ActiveTenantId}' has invalid format - potential injection vector!");
    }
}

public sealed class PropagationTrace
{
    private static readonly ConcurrentDictionary<string, List<PropagationEntry>> _traces = new();

    public string CorrelationId { get; init; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; }
    public string Operation { get; }
    public DateTime Timestamp { get; }
    public string ThreadId { get; }
    public string? ContinuityPoint { get; }

    public PropagationTrace(string tenantId, string operation, string? correlationId = null, string? continuityPoint = null)
    {
        TenantId = tenantId;
        Operation = operation;
        if (correlationId != null) CorrelationId = correlationId;
        Timestamp = DateTime.UtcNow;
        ThreadId = Environment.CurrentManagedThreadId.ToString();
        ContinuityPoint = continuityPoint;
    }

    public static void Record(PropagationTrace trace)
    {
        _traces.AddOrUpdate(
            trace.CorrelationId,
            _ => new List<PropagationEntry> { trace.ToEntry() },
            (_, list) =>
            {
                lock (list) { list.Add(trace.ToEntry()); }
                return list;
            });
    }

    public static IReadOnlyList<PropagationEntry> GetTrace(string correlationId)
    {
        return _traces.TryGetValue(correlationId, out var entries)
            ? entries.AsReadOnly()
            : Array.Empty<PropagationEntry>();
    }

    private PropagationEntry ToEntry() => new(
        TenantId,
        Operation,
        Timestamp,
        ThreadId,
        ContinuityPoint);

    public static void Clear() => _traces.Clear();

    public static void Clear(string correlationId) => _traces.TryRemove(correlationId, out _);
}

public readonly record struct PropagationEntry(
    string TenantId,
    string Operation,
    DateTime Timestamp,
    string ThreadId,
    string? ContinuityPoint);

public sealed class TenantContextCollector
{
    private readonly ConcurrentQueue<string> _operations = new();

    public string CorrelationId { get; } = Guid.NewGuid().ToString("N");

    public void Record(string tenantId, string operation, string? continuityPoint = null)
    {
        // Try to verify against actual AsyncLocal context if available
        var context = Karamchari.Core.Multitenancy.Execution.TenantExecutionContext.Current;
        var actualTenantId = context?.TenantId;
        var reportedTenantId = tenantId;

        var trace = new PropagationTrace(reportedTenantId, operation, context?.CorrelationId ?? this.CorrelationId, continuityPoint);
        PropagationTrace.Record(trace);

        // We log BOTH the reported (expected) and actual (AsyncLocal) tenant IDs
        _operations.Enqueue($"[{operation}] Expected={reportedTenantId} Actual={actualTenantId ?? "NULL"} @ {DateTime.UtcNow:O}");
    }

    public string[] GetOperations() => _operations.ToArray();

    public bool HasContamination()
    {
        var ops = _operations.ToArray();
        if (ops.Length < 1) return false;

        // Contamination occurs if ANY operation has an Actual tenant that doesn't match its Expected tenant
        foreach (var op in ops)
        {
            var expected = ExtractValue(op, "Expected=");
            var actual = ExtractValue(op, "Actual=");

            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string ExtractValue(string op, string prefix)
    {
        var start = op.IndexOf(prefix, StringComparison.Ordinal);
        if (start < 0) return "unknown";

        start += prefix.Length;
        var end = op.IndexOf(' ', start);
        if (end < 0) end = op.Length;

        return op.Substring(start, end - start);
    }
}

public sealed class ChaosIntent
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required ChaosCategory Category { get; init; }
    public required ChaosSeverity Severity { get; init; }
    public Dictionary<string, string> Parameters { get; init; } = new();
    public DateTime InjectedAt { get; init; } = DateTime.UtcNow;
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
    RlsBypass
}

public enum ChaosSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public sealed class TenantAttackVector
{
    public required string Name { get; init; }
    public required string Category { get; init; }
    public required string ExploitPath { get; init; }
    public required bool Exploitable { get; init; }
    public string? ProofOfConcept { get; init; }
    public ChaosSeverity Severity { get; init; } = ChaosSeverity.Medium;
    public string? Mitigation { get; init; }
}

public sealed class TenantLeakEvidence
{
    public required string Vector { get; init; }
    public required string Severity { get; init; }
    public required string ReproductionSteps { get; init; }
    public required string RootCause { get; init; }
    public required string Exploitability { get; init; }
    public required string OperationalImpact { get; init; }
    public string? Remediation { get; init; }
}

public static class TenantTestDataGenerator
{
    private static readonly string[] ValidTenantPrefixes = { "acme", "globex", "initech", "umbrella", "waystar" };
    private static readonly string[] MaliciousPatterns = { "admin", "dbo", "dbo--", "tenant_", "sys", "guest", "../" };
    private static int _counter;

    public static string GenerateTenantId() => $"{ValidTenantPrefixes[_counter++ % ValidTenantPrefixes.Length]}_{Guid.NewGuid().ToString("N")[..8]}";

    public static string GenerateSchemaName(string tenantId) => $"tenant_{tenantId.ToLowerInvariant()}";

    public static IEnumerable<string> GenerateMaliciousTenantIds()
    {
        foreach (var pattern in MaliciousPatterns)
            yield return pattern;
        yield return new string('a', 1000);
        yield return "\u0000\u0001";
        yield return "tenant_a'; DROP TABLE Users;--";
        yield return "../../../etc/passwd";
    }

    public static IEnumerable<string> GenerateFuzzedCacheKeys()
    {
        yield return "tenant_";
        yield return "_tenant";
        yield return "tenantacme";
        yield return "tenantacme_data";
        yield return "tenant::acme::data";
        yield return "tenant/acme/data";
        yield return "tenant\x00acme";
        yield return "tenant\u200bacme";
        yield return "tenant%E2%80%8Bacme";
    }
}
