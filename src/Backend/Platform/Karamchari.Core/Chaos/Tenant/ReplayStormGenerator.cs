using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Chaos.Tenant;

public sealed partial class ReplayStormGenerator
{
    private readonly ILogger<ReplayStormGenerator> _logger;
    private readonly ConcurrentDictionary<string, ReplayStormState> _stormStates = new();
    private static readonly Random _random = new();

    private sealed class ReplayStormState
    {
        public ConcurrentDictionary<string, DateTime> ProcessedMessages { get; } = new();
        public ConcurrentDictionary<string, int> DuplicateCounts { get; } = new();
        public ConcurrentDictionary<string, DateTime> StaleMessages { get; } = new();
        public long TotalReplaysDetected { get; set; }
        public long TotalStaleDeliveries { get; set; }
        public long TotalDuplicateDeliveries { get; set; }
    }

    public ReplayStormGenerator(ILogger<ReplayStormGenerator> logger)
    {
        _logger = logger;
    }

    public async Task GenerateReplayAttackAsync(
        string tenantId,
        IReadOnlyList<string> messageIds,
        TimeSpan replayWindow)
    {
        var normalizedTenantId = tenantId.ToLowerInvariant();
        var state = GetOrCreateState(normalizedTenantId);

        LogGeneratingReplayAttack(_logger, normalizedTenantId, messageIds.Count);

        foreach (var messageId in messageIds)
        {
            state.ProcessedMessages.TryAdd(messageId, DateTime.UtcNow);
        }

        await Task.Delay(_random.Next(100, 300));

        var replayAttempts = messageIds.Take(messageIds.Count / 2).ToList();

        foreach (var replayedMessage in replayAttempts)
        {
            var isDuplicate = state.ProcessedMessages.ContainsKey(replayedMessage);

            if (isDuplicate)
            {
                state.DuplicateCounts.AddOrUpdate(
                    replayedMessage,
                    1,
                    (_, count) => count + 1);

                state.TotalReplaysDetected++;
                state.TotalDuplicateDeliveries++;

                LogDuplicateMessageDetected(_logger, replayedMessage, normalizedTenantId);
            }
        }

        LogAttackCompleted(_logger, state.TotalReplaysDetected, state.TotalDuplicateDeliveries);
    }

    public async Task GenerateStaleDeliveryAsync(
        string tenantId,
        IReadOnlyList<string> messageIds,
        TimeSpan messageAge)
    {
        var normalizedTenantId = tenantId.ToLowerInvariant();
        var state = GetOrCreateState(normalizedTenantId);
        var staleTimestamp = DateTime.UtcNow - messageAge;

        LogGeneratingStaleDelivery(_logger, normalizedTenantId);

        foreach (var messageId in messageIds)
        {
            state.ProcessedMessages.TryAdd(messageId, staleTimestamp);
            state.StaleMessages.TryAdd(messageId, staleTimestamp);
            state.TotalStaleDeliveries++;
        }

        await Task.Delay(_random.Next(50, 150));

        LogStaleDeliveryCompleted(_logger, state.TotalStaleDeliveries);
    }

    public async Task GenerateDuplicateDeliveryAsync(
        string tenantId,
        IReadOnlyList<string> messageIds,
        int duplicateCount)
    {
        var normalizedTenantId = tenantId.ToLowerInvariant();
        var state = GetOrCreateState(normalizedTenantId);

        LogGeneratingDuplicateDelivery(_logger, normalizedTenantId);

        foreach (var messageId in messageIds)
        {
            state.ProcessedMessages.TryAdd(messageId, DateTime.UtcNow);

            for (int i = 0; i < duplicateCount; i++)
            {
                state.DuplicateCounts.AddOrUpdate(
                    messageId,
                    1,
                    (_, count) => count + 1);

                state.TotalDuplicateDeliveries++;
            }
        }

        LogDuplicateDeliveryCompleted(_logger, messageIds.Count, duplicateCount);
    }

    public async Task GenerateReplayTimingTestAsync(
        string tenantId,
        IReadOnlyList<string> messageIds,
        TimeSpan timingWindow)
    {
        var normalizedTenantId = tenantId.ToLowerInvariant();
        var state = GetOrCreateState(normalizedTenantId);

        LogTestingReplayTiming(_logger, normalizedTenantId);

        foreach (var messageId in messageIds)
        {
            state.ProcessedMessages.TryAdd(messageId, DateTime.UtcNow);
        }

        var baseDelay = timingWindow.TotalMilliseconds / 2;

        for (int i = 0; i < 3; i++)
        {
            var delayMs = baseDelay * (i + 1);
            await Task.Delay(TimeSpan.FromMilliseconds(delayMs));

            var testMessageIds = messageIds.Take(messageIds.Count / 3).ToList();

            foreach (var messageId in testMessageIds)
            {
                var isReplay = state.ProcessedMessages.TryGetValue(messageId, out var timestamp);

                if (isReplay)
                {
                    var age = DateTime.UtcNow - timestamp;
                    var withinWindow = age < timingWindow;

                    if (withinWindow)
                    {
                        state.TotalReplaysDetected++;
                    }
                }
            }
        }

        LogTimingTestCompleted(_logger, state.TotalReplaysDetected);
    }

    public ReplayStormReport GetStormReport(string tenantId)
    {
        var normalizedTenantId = tenantId.ToLowerInvariant();

        if (!_stormStates.TryGetValue(normalizedTenantId, out var state))
        {
            return new ReplayStormReport(
                normalizedTenantId,
                0,
                0,
                0,
                0,
                Array.Empty<string>());
        }

        var duplicateMessages = state.DuplicateCounts
            .Where(kvp => kvp.Value > 1)
            .Select(kvp => kvp.Key)
            .ToArray();

        return new ReplayStormReport(
            normalizedTenantId,
            state.ProcessedMessages.Count,
            state.TotalReplaysDetected,
            state.TotalStaleDeliveries,
            state.TotalDuplicateDeliveries,
            duplicateMessages);
    }

    public void ClearStormState(string tenantId)
    {
        var normalizedTenantId = tenantId.ToLowerInvariant();
        _stormStates.TryRemove(normalizedTenantId, out _);
    }

    public void ClearAllStormStates()
    {
        _stormStates.Clear();
    }

    private ReplayStormState GetOrCreateState(string tenantId)
    {
        return _stormStates.GetOrAdd(tenantId, _ => new ReplayStormState());
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "REPLAY STORM: Generating replay attack for tenant {TenantId} with {MessageCount} messages")]
    static partial void LogGeneratingReplayAttack(ILogger logger, string tenantId, int messageCount);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "REPLAY STORM: Duplicate message detected {MessageId} for tenant {TenantId}")]
    static partial void LogDuplicateMessageDetected(ILogger logger, string messageId, string tenantId);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "REPLAY STORM: Attack completed. Replays detected: {Replays}, Duplicates: {Duplicates}")]
    static partial void LogAttackCompleted(ILogger logger, long replays, long duplicates);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "REPLAY STORM: Generating stale message delivery for tenant {TenantId}")]
    static partial void LogGeneratingStaleDelivery(ILogger logger, string tenantId);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "REPLAY STORM: Stale delivery completed. Stale messages: {Count}")]
    static partial void LogStaleDeliveryCompleted(ILogger logger, long count);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Warning,
        Message = "REPLAY STORM: Generating duplicate delivery for tenant {TenantId}")]
    static partial void LogGeneratingDuplicateDelivery(ILogger logger, string tenantId);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Information,
        Message = "REPLAY STORM: Duplicate delivery completed. Messages: {Count}, Duplicates per message: {DuplicateCount}")]
    static partial void LogDuplicateDeliveryCompleted(ILogger logger, int count, int duplicateCount);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Information,
        Message = "REPLAY STORM: Testing replay timing window for tenant {TenantId}")]
    static partial void LogTestingReplayTiming(ILogger logger, string tenantId);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Information,
        Message = "REPLAY STORM: Timing test completed. Replays detected: {Count}")]
    static partial void LogTimingTestCompleted(ILogger logger, long count);
}

public sealed record ReplayStormReport(
    string TenantId,
    long TotalMessagesProcessed,
    long TotalReplaysDetected,
    long TotalStaleDeliveries,
    long TotalDuplicateDeliveries,
    string[] DuplicateMessageIds);
