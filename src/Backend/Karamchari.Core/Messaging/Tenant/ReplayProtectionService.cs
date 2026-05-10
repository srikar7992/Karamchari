using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Messaging.Tenant;

/// <summary>
/// Service that provides replay protection by tracking message content hashes and original message IDs.
/// Prevents duplicate message processing within a configurable time window.
/// </summary>
public sealed class ReplayProtectionService
{
    private readonly ConcurrentDictionary<string, ReplayEntry> _idempotencyCache = new();
    private readonly ILogger<ReplayProtectionService> _logger;
    private readonly TimeSpan _replayWindow;

    private record ReplayEntry(DateTime Timestamp, string? ContentHash);

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayProtectionService"/> class.
    /// </summary>
    /// <param name="logger">The logger for replay protection events.</param>
    /// <param name="replayWindow">The time window during which replays are detected (default: 24 hours).</param>
    public ReplayProtectionService(ILogger<ReplayProtectionService> logger, TimeSpan? replayWindow = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _replayWindow = replayWindow ?? TimeSpan.FromHours(24);
    }

    /// <summary>
    /// Checks if the message is a replay (has already been processed within the replay window).
    /// </summary>
    /// <param name="messageId">The original message identifier.</param>
    /// <param name="contentHash">Optional content hash for additional validation.</param>
    /// <returns>True if this is a replay that should be rejected; false if this is a new message.</returns>
    public bool IsReplay(string messageId, string? contentHash = null)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Replay check called with null or empty message ID, treating as new message");
            }
            return false;
        }

        var key = BuildCacheKey(messageId, contentHash);

        if (_idempotencyCache.TryGetValue(key, out var entry))
        {
            var age = DateTime.UtcNow - entry.Timestamp;

            if (age > _replayWindow)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(
                        "Replay entry for message {MessageId} is stale (age: {Age}), removing and treating as new",
                        messageId, age);
                }
                _idempotencyCache.TryRemove(key, out _);
                return false;
            }

            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "Replay detected for message {MessageId} with content hash {ContentHash}, age: {Age}",
                    messageId, contentHash ?? "N/A", age);
            }

            return true;
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("No replay detected for message {MessageId}", messageId);
        }

        return false;
    }

    /// <summary>
    /// Records a message as processed to enable future replay detection.
    /// </summary>
    /// <param name="messageId">The original message identifier.</param>
    /// <param name="contentHash">Optional content hash for additional validation.</param>
    public void RecordProcessed(string messageId, string? contentHash = null)
    {
        if (string.IsNullOrWhiteSpace(messageId)) return;

        var key = BuildCacheKey(messageId, contentHash);
        var entry = new ReplayEntry(DateTime.UtcNow, contentHash);

        var existing = _idempotencyCache.AddOrUpdate(key, entry, (_, _) => entry);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Recorded message {MessageId} as processed, content hash: {ContentHash}",
                messageId, contentHash ?? "N/A");
        }
    }

    /// <summary>
    /// Computes the SHA256 content hash of a message.
    /// </summary>
    /// <param name="messageContent">The message content to hash.</param>
    /// <returns>The hexadecimal string representation of the SHA256 hash.</returns>
    public static string ComputeContentHash(string messageContent)
    {
        if (string.IsNullOrWhiteSpace(messageContent))
        {
            return string.Empty;
        }

        var bytes = Encoding.UTF8.GetBytes(messageContent);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Computes the SHA256 content hash of a byte array.
    /// </summary>
    /// <param name="messageBytes">The message bytes to hash.</param>
    /// <returns>The hexadecimal string representation of the SHA256 hash.</returns>
    public static string ComputeContentHash(byte[] messageBytes)
    {
        if (messageBytes == null || messageBytes.Length == 0)
        {
            return string.Empty;
        }

        var hashBytes = SHA256.HashData(messageBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Checks if the provided content hash matches the expected hash.
    /// </summary>
    /// <param name="providedHash">The hash provided in the message headers.</param>
    /// <param name="actualContent">The actual message content to hash and compare.</param>
    /// <returns>True if the hashes match; false otherwise.</returns>
    public static bool ValidateContentHash(string? providedHash, string actualContent)
    {
        if (string.IsNullOrWhiteSpace(providedHash)) return false;

        var computedHash = ComputeContentHash(actualContent);
        return string.Equals(providedHash, computedHash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Clears expired entries from the idempotency cache.
    /// </summary>
    public void ClearExpiredEntries()
    {
        var expiredKeys = _idempotencyCache
            .Where(kvp => (DateTime.UtcNow - kvp.Value.Timestamp) > _replayWindow)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _idempotencyCache.TryRemove(key, out _);
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Cleared {Count} expired replay protection entries", expiredKeys.Count);
        }
    }

    /// <summary>
    /// Gets the current count of tracked messages in the replay cache.
    /// </summary>
    public int TrackedMessageCount => _idempotencyCache.Count;

    private static string BuildCacheKey(string messageId, string? contentHash)
    {
        if (string.IsNullOrWhiteSpace(contentHash))
        {
            return $"msg:{messageId}";
        }
        return $"msg:{messageId}:hash:{contentHash}";
    }
}
