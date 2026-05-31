using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Chaos.Tenant;

public sealed partial class TenantChaosEngine
{
    private readonly ILogger<TenantChaosEngine> _logger;
    private readonly ConcurrentDictionary<string, ChaosState> _chaosStates = new();
    private static readonly Random _random = new();

    private sealed class ChaosState
    {
        public bool DbOutageEnabled { get; set; }
        public bool RedisOutageEnabled { get; set; }
        public TimeSpan? BrokerDelay { get; set; }
        public double PacketLossPercentage { get; set; }
        public (TimeSpan Min, TimeSpan Max)? RandomLatencyRange { get; set; }
        public bool DeadlockEnabled { get; set; }
        public bool ConnectionExhaustionEnabled { get; set; }
        public DateTime LastChaosAt { get; set; }
    }

    public TenantChaosEngine(ILogger<TenantChaosEngine> logger)
    {
        _logger = logger;
    }

    public async Task InjectDbOutageAsync(string tenantId)
    {
        var normalizedTenantId = NormalizeTenantId(tenantId);
        var state = GetOrCreateState(normalizedTenantId);

        state.DbOutageEnabled = true;
        state.LastChaosAt = DateTime.UtcNow;

        _logger.LogWarning(
            "CHAOS: Database outage injected for tenant {TenantId} at {Timestamp}",
            normalizedTenantId,
            state.LastChaosAt);

        await Task.Delay(_random.Next(100, 500));
    }

    public async Task InjectRedisOutageAsync(string tenantId)
    {
        var normalizedTenantId = NormalizeTenantId(tenantId);
        var state = GetOrCreateState(normalizedTenantId);

        state.RedisOutageEnabled = true;
        state.LastChaosAt = DateTime.UtcNow;

        _logger.LogWarning(
            "CHAOS: Redis outage injected for tenant {TenantId} at {Timestamp}",
            normalizedTenantId,
            state.LastChaosAt);

        await Task.Delay(_random.Next(100, 500));
    }

    public async Task InjectBrokerDelayAsync(string tenantId, TimeSpan delay)
    {
        var normalizedTenantId = NormalizeTenantId(tenantId);
        var state = GetOrCreateState(normalizedTenantId);

        state.BrokerDelay = delay;
        state.LastChaosAt = DateTime.UtcNow;

        _logger.LogWarning(
            "CHAOS: Message broker delay of {Delay} injected for tenant {TenantId}",
            delay,
            normalizedTenantId);

        await Task.Delay(_random.Next(50, 200));
    }

    public async Task InjectPacketLossAsync(string tenantId, double percentage)
    {
        var normalizedTenantId = NormalizeTenantId(tenantId);
        var state = GetOrCreateState(normalizedTenantId);

        state.PacketLossPercentage = Math.Clamp(percentage, 0, 100);
        state.LastChaosAt = DateTime.UtcNow;

        _logger.LogWarning(
            "CHAOS: Packet loss of {Percentage}% injected for tenant {TenantId}",
            percentage,
            normalizedTenantId);

        await Task.Delay(_random.Next(50, 200));
    }

    public async Task InjectRandomLatencyAsync(string tenantId, TimeSpan min, TimeSpan max)
    {
        var normalizedTenantId = NormalizeTenantId(tenantId);
        var state = GetOrCreateState(normalizedTenantId);

        state.RandomLatencyRange = (min, max);
        state.LastChaosAt = DateTime.UtcNow;

        var actualLatency = TimeSpan.FromTicks(
            min.Ticks + (long)((max.Ticks - min.Ticks) * _random.NextDouble()));

        _logger.LogWarning(
            "CHAOS: Random latency between {Min} and {Max} (actual: {Actual}) injected for tenant {TenantId}",
            min,
            max,
            actualLatency,
            normalizedTenantId);

        await Task.Delay(actualLatency);
    }

    public async Task InjectDeadlockAsync(string tenantId)
    {
        var normalizedTenantId = NormalizeTenantId(tenantId);
        var state = GetOrCreateState(normalizedTenantId);

        state.DeadlockEnabled = true;
        state.LastChaosAt = DateTime.UtcNow;

        _logger.LogWarning(
            "CHAOS: Simulated deadlock injected for tenant {TenantId} at {Timestamp}",
            normalizedTenantId,
            state.LastChaosAt);

        await Task.Delay(_random.Next(200, 800));
    }

    public async Task InjectConnectionExhaustionAsync(string tenantId)
    {
        var normalizedTenantId = NormalizeTenantId(tenantId);
        var state = GetOrCreateState(normalizedTenantId);

        state.ConnectionExhaustionEnabled = true;
        state.LastChaosAt = DateTime.UtcNow;

        _logger.LogWarning(
            "CHAOS: Connection exhaustion injected for tenant {TenantId} at {Timestamp}",
            normalizedTenantId,
            state.LastChaosAt);

        await Task.Delay(_random.Next(100, 400));
    }

    public bool IsTenantAffected(string tenantId, string chaosType)
    {
        var normalizedTenantId = NormalizeTenantId(tenantId);
        if (!_chaosStates.TryGetValue(normalizedTenantId, out var state))
        {
            return false;
        }

        return chaosType switch
        {
            "DbOutage" => state.DbOutageEnabled,
            "RedisOutage" => state.RedisOutageEnabled,
            "BrokerDelay" => state.BrokerDelay.HasValue,
            "PacketLoss" => state.PacketLossPercentage > 0,
            "RandomLatency" => state.RandomLatencyRange.HasValue,
            "Deadlock" => state.DeadlockEnabled,
            "ConnectionExhaustion" => state.ConnectionExhaustionEnabled,
            _ => false
        };
    }

    public void ClearChaosState(string tenantId)
    {
        var normalizedTenantId = NormalizeTenantId(tenantId);
        _chaosStates.TryRemove(normalizedTenantId, out _);

        _logger.LogInformation(
            "CHAOS: Cleared chaos state for tenant {TenantId}",
            normalizedTenantId);
    }

    public void ClearAllChaosStates()
    {
        _chaosStates.Clear();

        _logger.LogInformation(
            "CHAOS: Cleared all chaos states");
    }

    public IReadOnlyDictionary<string, DateTime> GetAffectedTenants()
    {
        return _chaosStates
            .Where(kvp => kvp.Value.LastChaosAt != DateTime.MinValue)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.LastChaosAt);
    }

    private ChaosState GetOrCreateState(string tenantId)
    {
        return _chaosStates.GetOrAdd(tenantId, _ => new ChaosState());
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "CHAOS: Database outage injected for tenant {TenantId} at {Timestamp}")]
    static partial void LogDbOutageInjected(ILogger logger, string tenantId, DateTime timestamp);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "CHAOS: Redis outage injected for tenant {TenantId} at {Timestamp}")]
    static partial void LogRedisOutageInjected(ILogger logger, string tenantId, DateTime timestamp);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "CHAOS: Message broker delay of {Delay} injected for tenant {TenantId}")]
    static partial void LogBrokerDelayInjected(ILogger logger, TimeSpan delay, string tenantId);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "CHAOS: Packet loss of {Percentage}% injected for tenant {TenantId}")]
    static partial void LogPacketLossInjected(ILogger logger, double percentage, string tenantId);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Warning,
        Message = "CHAOS: Random latency between {Min} and {Max} (actual: {Actual}) injected for tenant {TenantId}")]
    static partial void LogRandomLatencyInjected(ILogger logger, TimeSpan min, TimeSpan max, TimeSpan actual, string tenantId);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Warning,
        Message = "CHAOS: Simulated deadlock injected for tenant {TenantId} at {Timestamp}")]
    static partial void LogDeadlockInjected(ILogger logger, string tenantId, DateTime timestamp);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Warning,
        Message = "CHAOS: Connection exhaustion injected for tenant {TenantId} at {Timestamp}")]
    static partial void LogConnectionExhaustionInjected(ILogger logger, string tenantId, DateTime timestamp);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Information,
        Message = "CHAOS: Cleared chaos state for tenant {TenantId}")]
    static partial void LogClearedChaosState(ILogger logger, string tenantId);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Information,
        Message = "CHAOS: Cleared all chaos states")]
    static partial void LogClearedAllChaosStates(ILogger logger);

    private static string NormalizeTenantId(string tenantId)
    {
        return tenantId?.ToLowerInvariant() ?? string.Empty;
    }
}
