// -----------------------------------------------------------------------
// <copyright file="RetryStormGenerator.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Chaos.Tenant;

public sealed partial class RetryStormGenerator
{
    private readonly ILogger<RetryStormGenerator> _logger;
    private readonly ConcurrentDictionary<string, RetryStormState> _stormStates = new();
    private static readonly Random _random = new();

    private sealed class RetryStormState
    {
        public ConcurrentDictionary<string, int> RetryCounts { get; } = new();
        public ConcurrentDictionary<string, DateTime> LastRetryTime { get; } = new();
        public ConcurrentQueue<RetryEvent> RetryEvents { get; } = new();
        public long TotalRetriesAttempted { get; set; }
        public long TotalRetriesSucceeded { get; set; }
        public long TotalRetriesFailed { get; set; }
        public long TotalBackoffViolations { get; set; }
        public long TotalContextCorruptions { get; set; }
    }

    public sealed record RetryEvent(
        string MessageId,
        int AttemptNumber,
        DateTime Timestamp,
        TimeSpan Delay,
        bool Succeeded,
        string? Error);

    public RetryStormGenerator(ILogger<RetryStormGenerator> logger)
    {
        _logger = logger;
    }

    public async Task GenerateRetryStormAsync(
        string tenantId,
        IReadOnlyList<string> operationIds,
        int maxRetries,
        bool failFirstNAttempts = false)
    {
        var normalizedTenantId = tenantId.ToLowerInvariant();
        var state = GetOrCreateState(normalizedTenantId);

        LogGeneratingRetryStorm(_logger, normalizedTenantId, operationIds.Count, maxRetries);

        foreach (var operationId in operationIds)
        {
            var failedAttempts = failFirstNAttempts ? _random.Next(1, maxRetries) : 0;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                var delay = CalculateBackoffDelay(attempt);
                await Task.Delay(delay);

                var success = attempt > failedAttempts;
                var event_ = new RetryEvent(
                    operationId,
                    attempt,
                    DateTime.UtcNow,
                    delay,
                    success,
                    success ? null : "Simulated failure");

                state.RetryEvents.Enqueue(event_);
                state.RetryCounts.AddOrUpdate(operationId, attempt, (_, count) => Math.Max(count, attempt));
                state.LastRetryTime.TryAdd(operationId, DateTime.UtcNow);

                state.TotalRetriesAttempted++;

                if (success)
                {
                    state.TotalRetriesSucceeded++;
                    break;
                }
                else
                {
                    state.TotalRetriesFailed++;
                }

                if (attempt >= maxRetries)
                {
                    LogOperationExhaustedRetries(_logger, operationId, normalizedTenantId);
                }
            }
        }

        LogStormCompleted(_logger, state.TotalRetriesAttempted, state.TotalRetriesSucceeded, state.TotalRetriesFailed);
    }

    public async Task GenerateRetryTimingAttackAsync(
        string tenantId,
        IReadOnlyList<string> operationIds,
        TimeSpan backoffBase,
        double jitterPercentage)
    {
        var normalizedTenantId = tenantId.ToLowerInvariant();
        var state = GetOrCreateState(normalizedTenantId);

        LogGeneratingTimingAttack(_logger, normalizedTenantId);

        foreach (var operationId in operationIds)
        {
            for (int attempt = 1; attempt <= 5; attempt++)
            {
                var expectedDelay = backoffBase * Math.Pow(2, attempt - 1);
                var jitter = TimeSpan.FromMilliseconds(
                    expectedDelay.TotalMilliseconds * jitterPercentage * _random.NextDouble());

                var actualDelay = expectedDelay + jitter;

                var minRetryInterval = TimeSpan.FromMilliseconds(50);
                if (actualDelay < minRetryInterval)
                {
                    actualDelay = minRetryInterval;
                    state.TotalBackoffViolations++;
                }

                await Task.Delay(actualDelay);

                var event_ = new RetryEvent(
                    operationId,
                    attempt,
                    DateTime.UtcNow,
                    actualDelay,
                    true,
                    null);

                state.RetryEvents.Enqueue(event_);
                state.TotalRetriesAttempted++;
                state.TotalRetriesSucceeded++;
            }
        }

        LogTimingAttackCompleted(_logger, state.TotalBackoffViolations);
    }

    public async Task GenerateExponentialBackoffFailureAsync(
        string tenantId,
        IReadOnlyList<string> operationIds,
        bool immediateRetry = false)
    {
        var normalizedTenantId = tenantId.ToLowerInvariant();
        var state = GetOrCreateState(normalizedTenantId);

        LogGeneratingBackoffFailure(_logger, normalizedTenantId);

        foreach (var operationId in operationIds)
        {
            var baseDelay = immediateRetry ? TimeSpan.Zero : TimeSpan.FromMilliseconds(10);

            for (int attempt = 1; attempt <= 10; attempt++)
            {
                var delay = immediateRetry
                    ? TimeSpan.Zero
                    : baseDelay * Math.Pow(2, attempt - 1);

                await Task.Delay(delay);

                var backoffViolated = immediateRetry || (attempt > 1 && delay < baseDelay * Math.Pow(2, attempt - 2));
                if (backoffViolated && attempt > 1)
                {
                    state.TotalBackoffViolations++;
                }

                var event_ = new RetryEvent(
                    operationId,
                    attempt,
                    DateTime.UtcNow,
                    delay,
                    true,
                    null);

                state.RetryEvents.Enqueue(event_);
                state.TotalRetriesAttempted++;
                state.TotalRetriesSucceeded++;
            }
        }

        LogBackoffFailureCompleted(_logger, state.TotalBackoffViolations);
    }

    public async Task GenerateRetryContextCorruptionAsync(
        string tenantId,
        IReadOnlyList<string> operationIds)
    {
        var normalizedTenantId = tenantId.ToLowerInvariant();
        var state = GetOrCreateState(normalizedTenantId);

        LogGeneratingContextCorruption(_logger, normalizedTenantId);

        foreach (var operationId in operationIds)
        {
            var simulatedTenantIds = new[] { normalizedTenantId, "contaminated_tenant", "", "invalid_" };

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                var simulatedTenantId = simulatedTenantIds[_random.Next(simulatedTenantIds.Length)];

                var corrupted = simulatedTenantId != normalizedTenantId;

                if (corrupted)
                {
                    state.TotalContextCorruptions++;

                    LogContextCorruptionDetected(_logger, operationId, attempt, normalizedTenantId, simulatedTenantId);
                }

                await Task.Delay(_random.Next(10, 50));
            }
        }

        LogContextCorruptionCompleted(_logger, state.TotalContextCorruptions);
    }

    public RetryStormReport GetStormReport(string tenantId)
    {
        var normalizedTenantId = tenantId.ToLowerInvariant();

        if (!_stormStates.TryGetValue(normalizedTenantId, out var state))
        {
            return new RetryStormReport(
                normalizedTenantId,
                0,
                0,
                0,
                0,
                0);
        }

        return new RetryStormReport(
            normalizedTenantId,
            state.TotalRetriesAttempted,
            state.TotalRetriesSucceeded,
            state.TotalRetriesFailed,
            state.TotalBackoffViolations,
            state.TotalContextCorruptions);
    }

    public IReadOnlyList<RetryEvent> GetRetryEvents(string tenantId)
    {
        var normalizedTenantId = tenantId.ToLowerInvariant();

        if (!_stormStates.TryGetValue(normalizedTenantId, out var state))
        {
            return Array.Empty<RetryEvent>();
        }

        return state.RetryEvents.ToArray();
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

    private static TimeSpan CalculateBackoffDelay(int attempt)
    {
        var baseDelayMs = 100;
        var delay = baseDelayMs * Math.Pow(2, attempt - 1);
        var jitter = _random.NextDouble() * baseDelayMs;

        return TimeSpan.FromMilliseconds(delay + jitter);
    }

    private RetryStormState GetOrCreateState(string tenantId)
    {
        return _stormStates.GetOrAdd(tenantId, _ => new RetryStormState());
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "RETRY STORM: Generating retry storm for tenant {TenantId} with {OperationCount} operations, max {MaxRetries} retries")]
    static partial void LogGeneratingRetryStorm(ILogger logger, string tenantId, int operationCount, int maxRetries);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "RETRY STORM: Operation {OperationId} exhausted all retries for tenant {TenantId}")]
    static partial void LogOperationExhaustedRetries(ILogger logger, string operationId, string tenantId);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "RETRY STORM: Storm completed. Attempts: {Total}, Succeeded: {Succeeded}, Failed: {Failed}")]
    static partial void LogStormCompleted(ILogger logger, long total, long succeeded, long failed);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "RETRY STORM: Generating retry timing attack for tenant {TenantId}")]
    static partial void LogGeneratingTimingAttack(ILogger logger, string tenantId);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "RETRY STORM: Timing attack completed. Violations: {Violations}")]
    static partial void LogTimingAttackCompleted(ILogger logger, long violations);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Warning,
        Message = "RETRY STORM: Generating exponential backoff failure for tenant {TenantId}")]
    static partial void LogGeneratingBackoffFailure(ILogger logger, string tenantId);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Information,
        Message = "RETRY STORM: Backoff failure test completed. Violations: {Violations}")]
    static partial void LogBackoffFailureCompleted(ILogger logger, long violations);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Warning,
        Message = "RETRY STORM: Generating retry context corruption for tenant {TenantId}")]
    static partial void LogGeneratingContextCorruption(ILogger logger, string tenantId);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Error,
        Message = "RETRY STORM: Context corruption detected for operation {OperationId} on attempt {Attempt}: tenant changed from {Original} to {Corrupted}")]
    static partial void LogContextCorruptionDetected(ILogger logger, string operationId, int attempt, string original, string corrupted);

    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Information,
        Message = "RETRY STORM: Context corruption test completed. Corruptions: {Corruptions}")]
    static partial void LogContextCorruptionCompleted(ILogger logger, long corruptions);
}

public sealed record RetryStormReport(
    string TenantId,
    long TotalRetriesAttempted,
    long TotalRetriesSucceeded,
    long TotalRetriesFailed,
    long TotalBackoffViolations,
    long TotalContextCorruptions);
