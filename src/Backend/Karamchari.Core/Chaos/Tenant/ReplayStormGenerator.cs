using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Chaos.Tenant;

public sealed class ReplayStormGenerator
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

        _logger.LogWarning(
            "REPLAY STORM: Generating replay attack for tenant {TenantId} with {MessageCount} messages",
            normalizedTenantId,
            messageIds.Count);

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

                _logger.LogWarning(
                    "REPLAY STORM: Duplicate message detected {MessageId} for tenant {TenantId}",
                    replayedMessage,
                    normalizedTenantId);
            }
        }

        _logger.LogInformation(
            "REPLAY STORM: Attack completed. Replays detected: {Replays}, Duplicates: {Duplicates}",
            state.TotalReplaysDetected,
            state.TotalDuplicateDeliveries);
    }

    public async Task GenerateStaleDeliveryAsync(
        string tenantId,
        IReadOnlyList<string> messageIds,
        TimeSpan messageAge)
    {
        var normalizedTenantId = tenantId.ToLowerInvariant();
        var state = GetOrCreateState(normalizedTenantId);
        var staleTimestamp = DateTime.UtcNow - messageAge;

        _logger.LogWarning(
            "REPLAY STORM: Generating stale message delivery for tenant {TenantId}",
            normalizedTenantId);

        foreach (var messageId in messageIds)
        {
            state.ProcessedMessages.TryAdd(messageId, staleTimestamp);
            state.StaleMessages.TryAdd(messageId, staleTimestamp);
            state.TotalStaleDeliveries++;
        }

        await Task.Delay(_random.Next(50, 150));

        _logger.LogInformation(
            "REPLAY STORM: Stale delivery completed. Stale messages: {Count}",
            state.TotalStaleDeliveries);
    }

    public async Task GenerateDuplicateDeliveryAsync(
        string tenantId,
        IReadOnlyList<string> messageIds,
        int duplicateCount)
    {
        var normalizedTenantId = tenantId.ToLowerInvariant();
        var state = GetOrCreateState(normalizedTenantId);

        _logger.LogWarning(
            "REPLAY STORM: Generating duplicate delivery for tenant {TenantId}",
            normalizedTenantId);

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

        _logger.LogInformation(
            "REPLAY STORM: Duplicate delivery completed. Messages: {Count}, Duplicates per message: {DuplicateCount}",
            messageIds.Count,
            duplicateCount);
    }

    public async Task GenerateReplayTimingTestAsync(
        string tenantId,
        IReadOnlyList<string> messageIds,
        TimeSpan timingWindow)
    {
        var normalizedTenantId = tenantId.ToLowerInvariant();
        var state = GetOrCreateState(normalizedTenantId);

        _logger.LogInformation(
            "REPLAY STORM: Testing replay timing window for tenant {TenantId}",
            normalizedTenantId);

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

        _logger.LogInformation(
            "REPLAY STORM: Timing test completed. Replays detected: {Count}",
            state.TotalReplaysDetected);
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
}

public sealed record ReplayStormReport(
    string TenantId,
    long TotalMessagesProcessed,
    long TotalReplaysDetected,
    long TotalStaleDeliveries,
    long TotalDuplicateDeliveries,
    string[] DuplicateMessageIds);
