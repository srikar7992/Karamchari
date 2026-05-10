using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Security.Tenant;

public sealed class ReplayAttackValidator
{
    private readonly ILogger<ReplayAttackValidator> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _processedMessages = new();
    private readonly ConcurrentDictionary<string, DateTime> _staleMessages = new();
    private readonly ConcurrentDictionary<string, DateTime> _duplicateMessages = new();

    public ReplayAttackValidator(ILogger<ReplayAttackValidator> logger)
    {
        _logger = logger;
    }

    public bool ValidateReplayProtection(
        string messageId,
        string? contentHash = null,
        DateTime? messageTimestamp = null)
    {
        if (_processedMessages.ContainsKey(messageId))
        {
            _duplicateMessages.TryAdd(messageId, DateTime.UtcNow);

            _logger.LogWarning(
                "REPLAY ATTACK: Duplicate message detected: {MessageId}",
                messageId);

            return false;
        }

        if (messageTimestamp.HasValue)
        {
            var messageAge = DateTime.UtcNow - messageTimestamp.Value;

            if (messageAge.TotalHours > 1)
            {
                _staleMessages.TryAdd(messageId, DateTime.UtcNow);

                _logger.LogWarning(
                    "REPLAY ATTACK: Stale message detected: {MessageId}, age: {Age}",
                    messageId,
                    messageAge);

                return false;
            }
        }

        _processedMessages.TryAdd(messageId, DateTime.UtcNow);
        return true;
    }

    public bool DetectStaleReplay(string messageId, TimeSpan maxAge)
    {
        if (_processedMessages.TryGetValue(messageId, out var timestamp))
        {
            var age = DateTime.UtcNow - timestamp;

            if (age > maxAge)
            {
                _staleMessages.TryAdd(messageId, DateTime.UtcNow);

                _logger.LogWarning(
                    "STALE REPLAY: Message {MessageId} is stale (age: {Age})",
                    messageId,
                    age);

                return true;
            }

            return false;
        }

        return false;
    }

    public bool HandleDuplicateReplay(string messageId)
    {
        if (_processedMessages.ContainsKey(messageId))
        {
            _duplicateMessages.TryAdd(messageId, DateTime.UtcNow);

            _logger.LogWarning(
                "DUPLICATE REPLAY: Message {MessageId} is a duplicate",
                messageId);

            return false;
        }

        _processedMessages.TryAdd(messageId, DateTime.UtcNow);
        return true;
    }

    public void RecordMessage(string messageId)
    {
        _processedMessages.TryAdd(messageId, DateTime.UtcNow);
    }

    public void RecordStaleMessage(string messageId, DateTime timestamp)
    {
        _processedMessages.TryAdd(messageId, timestamp);
        _staleMessages.TryAdd(messageId, timestamp);
    }

    public ReplayValidationReport GetReport()
    {
        return new ReplayValidationReport(
            _processedMessages.Count,
            _staleMessages.Count,
            _duplicateMessages.Count);
    }

    public void Clear()
    {
        _processedMessages.Clear();
        _staleMessages.Clear();
        _duplicateMessages.Clear();
    }
}

public sealed record ReplayValidationReport(
    int TotalMessagesProcessed,
    int StaleReplaysDetected,
    int DuplicateReplaysDetected);
