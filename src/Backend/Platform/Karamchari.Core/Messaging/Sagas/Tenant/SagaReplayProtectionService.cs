// -----------------------------------------------------------------------
// <copyright file="SagaReplayProtectionService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Messaging.Sagas.Tenant;

/// <summary>
///     Protection service against saga replay attacks, providing duplicate detection
///     and stale correlation prevention to ensure saga integrity.
/// </summary>
public interface ISagaReplayProtectionService
{
    /// <summary>
    ///     Checks if a message has already been processed for the given saga instance.
    /// </summary>
    /// <param name="sagaId">The saga instance identifier.</param>
    /// <param name="messageId">The message identifier to check.</param>
    /// <returns>
    ///     <c>true</c> if the message was already processed; otherwise, <c>false</c>.
    /// </returns>
    bool IsDuplicate(Guid sagaId, Guid messageId);

    /// <summary>
    ///     Marks a message as processed for the given saga instance.
    /// </summary>
    /// <param name="sagaId">The saga instance identifier.</param>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="expiresAt">Optional expiration time for the tracking entry.</param>
    void MarkProcessed(Guid sagaId, Guid messageId, DateTime? expiresAt = null);

    /// <summary>
    ///     Detects if a correlation is stale (saga completed or too old).
    /// </summary>
    /// <param name="sagaId">The saga instance identifier.</param>
    /// <param name="sagaCreatedAt">When the saga was created.</param>
    /// <param name="maxAge">Maximum allowed age for a correlation.</param>
    /// <returns>
    ///     <c>true</c> if the correlation is stale; otherwise, <c>false</c>.
    /// </returns>
    bool IsStaleCorrelation(Guid sagaId, DateTime sagaCreatedAt, TimeSpan maxAge);

    /// <summary>
    ///     Checks if a saga instance is orphaned (exists but no activity).
    /// </summary>
    /// <param name="sagaId">The saga instance identifier.</param>
    /// <param name="lastActivityAt">The last activity timestamp.</param>
    /// <param name="threshold">The threshold duration to consider orphaned.</param>
    /// <returns>
    ///     <c>true</c> if the saga is orphaned; otherwise, <c>false</c>.
    /// </returns>
    bool IsOrphanedSaga(Guid sagaId, DateTime lastActivityAt, TimeSpan threshold);

    /// <summary>
    ///     Clears all tracking data for a saga instance.
    /// </summary>
    /// <param name="sagaId">The saga instance identifier to clear.</param>
    void ClearSagaTracking(Guid sagaId);
}

/// <summary>
///     Result of replay detection check.
/// </summary>
public sealed class SagaReplayDetectionResult
{
    /// <summary>
    ///     Gets a value indicating whether this is a duplicate message.
    /// </summary>
    public bool IsDuplicate { get; init; }

    /// <summary>
    ///     Gets the number of times this message has been seen.
    /// </summary>
    public int SeenCount { get; init; }

    /// <summary>
    ///     Gets the first time this message was processed.
    /// </summary>
    public DateTime? FirstProcessedAt { get; init; }

    /// <summary>
    ///     Gets the last time this message was processed.
    /// </summary>
    public DateTime? LastProcessedAt { get; init; }

    /// <summary>
    ///     Gets the saga instance ID.
    /// </summary>
    public Guid SagaId { get; init; }

    /// <summary>
    ///     Gets the message ID.
    /// </summary>
    public Guid MessageId { get; init; }

    /// <summary>
    ///     Gets the replay protection result.
    /// </summary>
    public static SagaReplayDetectionResult Duplicate(Guid sagaId, Guid messageId, int seenCount) => new()
    {
        IsDuplicate = true,
        SeenCount = seenCount,
        SagaId = sagaId,
        MessageId = messageId
    };

    /// <summary>
    ///     Creates a new message result.
    /// </summary>
    public static SagaReplayDetectionResult New(Guid sagaId, Guid messageId) => new()
    {
        IsDuplicate = false,
        SeenCount = 1,
        SagaId = sagaId,
        MessageId = messageId
    };
}

/// <summary>
///     Default implementation of saga replay protection service.
/// </summary>
public sealed class SagaReplayProtectionService : ISagaReplayProtectionService
{
    private readonly ConcurrentDictionary<string, SagaMessageTracking> _trackingStore = new();
    private readonly ConcurrentDictionary<Guid, SagaOrphanStatus> _orphanTracker = new();
    private readonly ILogger<SagaReplayProtectionService> _logger;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(24);

    public SagaReplayProtectionService(ILogger<SagaReplayProtectionService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsDuplicate(Guid sagaId, Guid messageId)
    {
        var key = GetTrackingKey(sagaId, messageId);

        if (_trackingStore.TryGetValue(key, out var tracking))
        {
            _logger.LogWarning(
                "SagaReplayProtection: Duplicate detected for saga {SagaId} message {MessageId}. " +
                "Seen count: {SeenCount}",
                sagaId,
                messageId,
                tracking.SeenCount + 1);
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public void MarkProcessed(Guid sagaId, Guid messageId, DateTime? expiresAt = null)
    {
        var key = GetTrackingKey(sagaId, messageId);
        var expiration = expiresAt ?? DateTime.UtcNow.Add(_defaultExpiration);
        var now = DateTime.UtcNow;

        _trackingStore.AddOrUpdate(
            key,
            _ => new SagaMessageTracking
            {
                SagaId = sagaId,
                MessageId = messageId,
                SeenCount = 1,
                FirstProcessedAt = now,
                LastProcessedAt = now,
                ExpiresAt = expiration
            },
            (_, existing) =>
            {
                existing.SeenCount++;
                existing.LastProcessedAt = now;
                return existing;
            });

        UpdateOrphanStatus(sagaId, now);

        _logger.LogDebug(
            "SagaReplayProtection: Marked message {MessageId} for saga {SagaId} as processed",
            messageId,
            sagaId);
    }

    /// <inheritdoc />
    public bool IsStaleCorrelation(Guid sagaId, DateTime sagaCreatedAt, TimeSpan maxAge)
    {
        var age = DateTime.UtcNow - sagaCreatedAt;
        var isStale = age > maxAge;

        if (isStale)
        {
            _logger.LogWarning(
                "SagaReplayProtection: Stale correlation detected for saga {SagaId}. " +
                "Age: {Age}, Max age: {MaxAge}",
                sagaId,
                age,
                maxAge);
        }

        return isStale;
    }

    /// <inheritdoc />
    public bool IsOrphanedSaga(Guid sagaId, DateTime lastActivityAt, TimeSpan threshold)
    {
        var elapsed = DateTime.UtcNow - lastActivityAt;
        var isOrphaned = elapsed > threshold;

        if (isOrphaned)
        {
            _logger.LogWarning(
                "SagaReplayProtection: Orphaned saga detected {SagaId}. " +
                "Last activity: {LastActivity}, Elapsed: {Elapsed}",
                sagaId,
                lastActivityAt,
                elapsed);
        }

        return isOrphaned;
    }

    /// <inheritdoc />
    public void ClearSagaTracking(Guid sagaId)
    {
        var keysToRemove = _trackingStore.Keys
            .Where(k => k.StartsWith(sagaId.ToString(), StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _trackingStore.TryRemove(key, out _);
        }

        _orphanTracker.TryRemove(sagaId, out _);

        _logger.LogInformation(
            "SagaReplayProtection: Cleared tracking for saga {SagaId}. Removed {Count} entries",
            sagaId,
            keysToRemove.Count);
    }

    private void UpdateOrphanStatus(Guid sagaId, DateTime activityTime)
    {
        _orphanTracker.AddOrUpdate(
            sagaId,
            _ => new SagaOrphanStatus { SagaId = sagaId, LastActivityAt = activityTime },
            (_, status) =>
            {
                status.LastActivityAt = activityTime;
                status.ActivityCount++;
                return status;
            });
    }

    private static string GetTrackingKey(Guid sagaId, Guid messageId)
        => $"{sagaId}:{messageId}";

    private class SagaMessageTracking
    {
        public Guid SagaId { get; init; }
        public Guid MessageId { get; init; }
        public int SeenCount { get; set; }
        public DateTime FirstProcessedAt { get; init; }
        public DateTime LastProcessedAt { get; set; }
        public DateTime ExpiresAt { get; init; }
    }

    private class SagaOrphanStatus
    {
        public Guid SagaId { get; init; }
        public DateTime LastActivityAt { get; set; }
        public int ActivityCount { get; set; }
    }
}
