using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Text.Json;
using MassTransit;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Karamchari.Core.Messaging.Outbox;

/// <summary>
/// Production outbox relay: polls dbo.OutboxMessage in batches, publishes
/// each message to the configured MassTransit transport (Azure Service Bus),
/// and marks delivery only after a successful publish.
///
/// Architecture notes:
/// - Claims OutboxState rows using UPDLOCK + READPAST (SQL Server row-skip-lock
///   equivalent), safe for multiple concurrent relay instances.
/// - Per-process GUID ensures one relay never steals another's claimed batch.
/// - Retry state is in-memory; resets on restart (acceptable — MaxRetries
///   prevents infinite loops, and messages remain in the outbox across restarts).
/// - Dead letters land in dbo.OutboxDeadLetter after MaxRetries exhaustion.
/// - Does NOT depend on any domain service; pure infrastructure.
/// </summary>
public sealed partial class OutboxRelayService : BackgroundService
{
    // ── internal representation of a claimed OutboxMessage row ──────────────

    private sealed record OutboxMessageRow(
        long SequenceNumber,
        Guid MessageId,
        Guid OutboxId,
        string Body,
        string MessageType,
        string? DestinationAddress,
        string? Headers,
        DateTime SentTime,
        DateTime? ExpirationTime,
        string? ContentType,
        Guid? CorrelationId,
        Guid? ConversationId,
        Guid? InitiatorId,
        Guid? RequestId);

    // ── pipe impl: preserve original envelope identifiers on re-publish ──────

    private sealed class ContextHeaderPipe : IPipe<PublishContext>
    {
        private readonly OutboxMessageRow _row;
        internal ContextHeaderPipe(OutboxMessageRow row) => _row = row;

        public Task Send(PublishContext context)
        {
            context.MessageId = _row.MessageId;
            if (_row.CorrelationId.HasValue) context.CorrelationId = _row.CorrelationId;
            if (_row.ConversationId.HasValue) context.ConversationId = _row.ConversationId;
            if (_row.InitiatorId.HasValue) context.InitiatorId = _row.InitiatorId;
            if (_row.RequestId.HasValue) context.RequestId = _row.RequestId;
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context) { }
    }

    private sealed class SendContextHeaderPipe : IPipe<SendContext>
    {
        private readonly OutboxMessageRow _row;
        internal SendContextHeaderPipe(OutboxMessageRow row) => _row = row;

        public Task Send(SendContext context)
        {
            context.MessageId = _row.MessageId;
            if (_row.CorrelationId.HasValue) context.CorrelationId = _row.CorrelationId;
            if (_row.ConversationId.HasValue) context.ConversationId = _row.ConversationId;
            if (_row.InitiatorId.HasValue) context.InitiatorId = _row.InitiatorId;
            if (_row.RequestId.HasValue) context.RequestId = _row.RequestId;
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context) { }
    }

    // ── retry tracking ───────────────────────────────────────────────────────

    private sealed record RetryState(int Attempts, DateTimeOffset NextRetry);

    // ── fields ───────────────────────────────────────────────────────────────

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IBus _bus;
    private readonly OutboxRelayMetrics _metrics;
    private readonly IOptions<OutboxRelayOptions> _options;
    private readonly ILogger<OutboxRelayService> _logger;

    // Stable per-process GUID used as the claim token in dbo.OutboxState.LockId.
    private readonly Guid _instanceId = Guid.NewGuid();

    // In-memory retry registry: OutboxId → (attempt count, next eligible time).
    // Resets on restart — acceptable because MaxRetries still bounds total attempts.
    private readonly ConcurrentDictionary<Guid, RetryState> _retryRegistry = new();

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Initializes a new instance of <see cref="OutboxRelayService"/>.
    /// </summary>
    public OutboxRelayService(
        IServiceScopeFactory scopeFactory,
        IBus bus,
        OutboxRelayMetrics metrics,
        IOptions<OutboxRelayOptions> options,
        ILogger<OutboxRelayService> logger)
    {
        _scopeFactory = scopeFactory;
        _bus = bus;
        _metrics = metrics;
        _options = options;
        _logger = logger;
    }

    // ── main loop ────────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(_logger, _instanceId, _options.Value.BatchSize, _options.Value.PollingInterval);

        // On startup: release any stale lock rows from prior crashed instances.
        await TryReleaseStaleLockAsync(CancellationToken.None);

        while (!stoppingToken.IsCancellationRequested)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var processed = await ProcessBatchAsync(stoppingToken);
                _metrics.BatchCycles.Add(1);
                _metrics.BatchDurationMs.Record(sw.Elapsed.TotalMilliseconds);

                if (processed == 0)
                    await Task.Delay(_options.Value.PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogBatchLoopError(_logger, ex, _instanceId);
                _metrics.BatchDurationMs.Record(sw.Elapsed.TotalMilliseconds);
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken).ConfigureAwait(false);
            }
        }

        await TryReleaseOwnLocksAsync(CancellationToken.None);
        LogStopped(_logger, _instanceId);
    }

    // ── batch processing ─────────────────────────────────────────────────────

    private async Task<int> ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OutboxRelayDbContext>();

        await using var connection = (SqlConnection)db.Database.GetDbConnection();
        await connection.OpenAsync(ct);

        var claimed = await ClaimBatchAsync(connection, ct);
        if (claimed.Count == 0)
            return 0;

        LogClaimed(_logger, claimed.Count, _instanceId);

        var messages = await LoadMessagesAsync(connection, claimed, ct);

        var toRelease = claimed.Except(messages.Keys).ToList();
        if (toRelease.Count > 0)
            await ReleaseLocksAsync(connection, toRelease, ct);

        if (messages.Count == 0)
            return 0;

        var opts = _options.Value;
        var semaphore = new SemaphoreSlim(opts.MaxConcurrency);
        var tasks = messages.Select(async kv =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                await ProcessOutboxIdAsync(db, connection, kv.Key, kv.Value, ct);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return messages.Count;
    }

    // ── SQL helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Atomically claims up to BatchSize OutboxState rows by setting
    /// LockId = _instanceId. UPDLOCK + READPAST lets concurrent relay
    /// instances skip rows already claimed instead of blocking.
    /// </summary>
    private async Task<List<Guid>> ClaimBatchAsync(SqlConnection connection, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.OutboxState
            SET    LockId = @lockId
            OUTPUT INSERTED.OutboxId
            WHERE  OutboxId IN (
                SELECT TOP (@batchSize) OutboxId
                FROM   dbo.OutboxState WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE  LockId   IS NULL
                  AND  Delivered IS NULL
                ORDER BY Created ASC
            )
            """;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@lockId", SqlDbType.UniqueIdentifier) { Value = _instanceId });
        cmd.Parameters.Add(new SqlParameter("@batchSize", SqlDbType.Int) { Value = _options.Value.BatchSize });

        var ids = new List<Guid>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            ids.Add(reader.GetGuid(0));

        return ids;
    }

    private static async Task<Dictionary<Guid, List<OutboxMessageRow>>> LoadMessagesAsync(
        SqlConnection connection,
        List<Guid> outboxIds,
        CancellationToken ct)
    {
        var paramNames = outboxIds.Select((_, i) => $"@id{i}").ToArray();
        var sql = $"""
            SELECT SequenceNumber,
                   MessageId,
                   OutboxId,
                   Body,
                   MessageType,
                   DestinationAddress,
                   Headers,
                   SentTime,
                   ExpirationTime,
                   ContentType,
                   CorrelationId,
                   ConversationId,
                   InitiatorId,
                   RequestId
            FROM   dbo.OutboxMessage
            WHERE  OutboxId IN ({string.Join(",", paramNames)})
            ORDER  BY OutboxId, SequenceNumber ASC
            """;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        for (var i = 0; i < outboxIds.Count; i++)
            cmd.Parameters.Add(new SqlParameter(paramNames[i], SqlDbType.UniqueIdentifier) { Value = outboxIds[i] });

        var result = new Dictionary<Guid, List<OutboxMessageRow>>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            var row = new OutboxMessageRow(
                SequenceNumber:    reader.GetInt64(0),
                MessageId:         reader.GetGuid(1),
                OutboxId:          reader.GetGuid(2),
                Body:              reader.GetString(3),
                MessageType:       reader.GetString(4),
                DestinationAddress: reader.IsDBNull(5) ? null : reader.GetString(5),
                Headers:           reader.IsDBNull(6) ? null : reader.GetString(6),
                SentTime:          reader.GetDateTime(7),
                ExpirationTime:    reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                ContentType:       reader.IsDBNull(9) ? null : reader.GetString(9),
                CorrelationId:     reader.IsDBNull(10) ? null : reader.GetGuid(10),
                ConversationId:    reader.IsDBNull(11) ? null : reader.GetGuid(11),
                InitiatorId:       reader.IsDBNull(12) ? null : reader.GetGuid(12),
                RequestId:         reader.IsDBNull(13) ? null : reader.GetGuid(13));

            if (!result.TryGetValue(row.OutboxId, out var list))
            {
                list = [];
                result[row.OutboxId] = list;
            }
            list.Add(row);
        }

        return result;
    }

    // ── per-outbox-id processing ──────────────────────────────────────────────

    private async Task ProcessOutboxIdAsync(
        OutboxRelayDbContext db,
        SqlConnection connection,
        Guid outboxId,
        List<OutboxMessageRow> messages,
        CancellationToken ct)
    {
        var opts = _options.Value;

        if (_retryRegistry.TryGetValue(outboxId, out var state))
        {
            if (DateTimeOffset.UtcNow < state.NextRetry)
            {
                await ReleaseLocksAsync(connection, [outboxId], ct);
                return;
            }

            if (state.Attempts >= opts.MaxRetries)
            {
                LogDeadLettering(_logger, outboxId, opts.MaxRetries);
                await DeadLetterBatchAsync(db, connection, outboxId, messages, state.Attempts, ct);
                _retryRegistry.TryRemove(outboxId, out _);
                return;
            }
        }

        try
        {
            foreach (var msg in messages)
            {
                if (msg.ExpirationTime.HasValue && msg.ExpirationTime.Value < DateTime.UtcNow)
                {
                    LogSkippedExpired(_logger, msg.MessageId, outboxId, msg.ExpirationTime.Value);
                    continue;
                }

                await PublishMessageAsync(msg, ct);

                _metrics.MessagesProcessed.Add(1);
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    var shortType = FirstTypeShortName(msg.MessageType);
                    LogRelayed(_logger, msg.MessageId, shortType, outboxId);
                }
            }

            await MarkDeliveredAsync(connection, outboxId, ct);
            _retryRegistry.TryRemove(outboxId, out _);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            await ReleaseLocksAsync(connection, [outboxId], ct: CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            var attempts = (state?.Attempts ?? 0) + 1;
            var delay = ComputeRetryDelay(attempts, opts);

            _metrics.MessagesFailed.Add(1);
            LogPublishFailed(_logger, ex, outboxId, attempts, opts.MaxRetries, delay);

            _retryRegistry[outboxId] = new RetryState(attempts, DateTimeOffset.UtcNow + delay);
            await ReleaseLocksAsync(connection, [outboxId], ct: CancellationToken.None);

            if (attempts >= opts.MaxRetries)
            {
                LogDeadLetteringOnExhaustion(_logger, outboxId);
                await DeadLetterBatchAsync(db, connection, outboxId, messages, attempts, ct: CancellationToken.None);
                _retryRegistry.TryRemove(outboxId, out _);
            }
        }
    }

    // ── publish ───────────────────────────────────────────────────────────────

    private async Task PublishMessageAsync(OutboxMessageRow msg, CancellationToken ct)
    {
        if (msg.ContentType != null
            && !msg.ContentType.StartsWith("application/vnd.masstransit+json", StringComparison.OrdinalIgnoreCase)
            && !msg.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
        {
            LogPoisonContentType(_logger, msg.ContentType, msg.MessageId);
            return;
        }

        using var doc = JsonDocument.Parse(msg.Body);
        var root = doc.RootElement;

        if (!root.TryGetProperty("message", out var payloadElement))
        {
            LogPoisonMissingMessage(_logger, msg.MessageId);
            return;
        }

        var primaryUrn = msg.MessageType.Split(';', StringSplitOptions.TrimEntries)[0];
        var resolvedType = ResolveMessageType(primaryUrn);

        if (resolvedType is null)
        {
            LogPoisonUnresolvableType(_logger, primaryUrn, msg.MessageId);
            return;
        }

        var payload = payloadElement.Deserialize(resolvedType, _jsonOptions)
            ?? throw new InvalidOperationException(
                $"Deserialised null for {resolvedType.FullName}. MessageId={msg.MessageId}");

        if (!string.IsNullOrEmpty(msg.DestinationAddress)
            && Uri.TryCreate(msg.DestinationAddress, UriKind.Absolute, out var destUri))
        {
            var endpoint = await _bus.GetSendEndpoint(destUri);
            await endpoint.Send(payload, resolvedType, new SendContextHeaderPipe(msg), ct);
        }
        else
        {
            await _bus.Publish(payload, resolvedType, new ContextHeaderPipe(msg), ct);
        }
    }

    // ── mark delivered ────────────────────────────────────────────────────────

    private static async Task MarkDeliveredAsync(
        SqlConnection connection,
        Guid outboxId,
        CancellationToken ct)
    {
        await using var del = connection.CreateCommand();
        del.CommandText = "DELETE FROM dbo.OutboxMessage WHERE OutboxId = @id";
        del.Parameters.Add(new SqlParameter("@id", SqlDbType.UniqueIdentifier) { Value = outboxId });
        await del.ExecuteNonQueryAsync(ct);

        await using var upd = connection.CreateCommand();
        upd.CommandText = """
            UPDATE dbo.OutboxState
            SET    Delivered = SYSUTCDATETIME(),
                   LockId    = NULL
            WHERE  OutboxId = @id
            """;
        upd.Parameters.Add(new SqlParameter("@id", SqlDbType.UniqueIdentifier) { Value = outboxId });
        await upd.ExecuteNonQueryAsync(ct);
    }

    // ── lock release ──────────────────────────────────────────────────────────

    private async Task ReleaseLocksAsync(
        SqlConnection connection,
        List<Guid> outboxIds,
        CancellationToken ct)
    {
        if (outboxIds.Count == 0) return;

        var paramNames = outboxIds.Select((_, i) => $"@id{i}").ToArray();
        var sql = $"""
            UPDATE dbo.OutboxState
            SET    LockId = NULL
            WHERE  LockId = @lockId
              AND  OutboxId IN ({string.Join(",", paramNames)})
            """;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@lockId", SqlDbType.UniqueIdentifier) { Value = _instanceId });
        for (var i = 0; i < outboxIds.Count; i++)
            cmd.Parameters.Add(new SqlParameter(paramNames[i], SqlDbType.UniqueIdentifier) { Value = outboxIds[i] });

        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task TryReleaseOwnLocksAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<OutboxRelayDbContext>();
            await using var connection = (SqlConnection)db.Database.GetDbConnection();
            await connection.OpenAsync(ct);

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                UPDATE dbo.OutboxState
                SET    LockId = NULL
                WHERE  LockId    = @lockId
                  AND  Delivered IS NULL
                """;
            cmd.Parameters.Add(new SqlParameter("@lockId", SqlDbType.UniqueIdentifier) { Value = _instanceId });
            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (Exception ex)
        {
            LogReleaseOwnLocksFailed(_logger, ex, _instanceId);
        }
    }

    private async Task TryReleaseStaleLockAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<OutboxRelayDbContext>();
            await using var connection = (SqlConnection)db.Database.GetDbConnection();
            await connection.OpenAsync(ct);

            await using var cmd = connection.CreateCommand();
            // Rows with a non-null LockId whose OutboxState was created more than
            // StaleLockTimeout ago are assumed abandoned by a dead instance.
            cmd.CommandText = """
                UPDATE dbo.OutboxState
                SET    LockId = NULL
                WHERE  LockId   IS NOT NULL
                  AND  Delivered IS NULL
                  AND  Created  < DATEADD(SECOND, @staleSecs, SYSUTCDATETIME())
                """;
            cmd.Parameters.Add(new SqlParameter("@staleSecs", SqlDbType.Int)
            {
                Value = -(int)_options.Value.StaleLockTimeout.TotalSeconds
            });

            var released = await cmd.ExecuteNonQueryAsync(ct);
            if (released > 0)
                LogStaleLockReleased(_logger, released, _instanceId);
        }
        catch (Exception ex)
        {
            LogStaleLockReleaseFailed(_logger, ex, _instanceId);
        }
    }

    // ── dead-letter ───────────────────────────────────────────────────────────

    private async Task DeadLetterBatchAsync(
        OutboxRelayDbContext db,
        SqlConnection connection,
        Guid outboxId,
        List<OutboxMessageRow> messages,
        int retryCount,
        CancellationToken ct)
    {
        foreach (var msg in messages)
        {
            db.DeadLetterMessages.Add(new OutboxDeadLetterMessage
            {
                MessageId          = msg.MessageId,
                OutboxId           = outboxId,
                MessageType        = msg.MessageType,
                Body               = msg.Body,
                Headers            = msg.Headers,
                DestinationAddress = msg.DestinationAddress,
                FailureReason      = "Exhausted MaxRetries",
                RetryCount         = retryCount,
                FailedAt           = DateTime.UtcNow,
            });

            _metrics.MessagesDeadLettered.Add(1);
            LogDeadLettered(_logger, msg.MessageId, FirstTypeShortName(msg.MessageType), outboxId, retryCount);
        }

        await db.SaveChangesAsync(ct);
        await MarkDeliveredAsync(connection, outboxId, ct);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Converts a MassTransit URN to a fully-qualified .NET type name and searches
    /// all assemblies loaded into the current AppDomain.
    ///
    /// Assumption: all event types are in scope at relay startup because the Api
    /// host references every bounded context. Not cached — background poll only.
    /// </summary>
    private static Type? ResolveMessageType(string urn)
    {
        const string prefix = "urn:message:";
        if (!urn.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        // "urn:message:Karamchari.HR.Integration:EmployeeHired" → "Karamchari.HR.Integration.EmployeeHired"
        var dotName = urn[prefix.Length..].Replace(":", ".", StringComparison.Ordinal);

        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(a => a.GetType(dotName))
            .FirstOrDefault(t => t is not null);
    }

    private static string FirstTypeShortName(string messageType)
    {
        var first = messageType.Split(';', StringSplitOptions.TrimEntries)[0];
        var dot = first.LastIndexOf('.');
        return dot >= 0 ? first[(dot + 1)..] : first;
    }

    private static TimeSpan ComputeRetryDelay(int attempt, OutboxRelayOptions opts)
    {
        var seconds = opts.InitialRetryDelay.TotalSeconds * Math.Pow(2, attempt - 1);
        return TimeSpan.FromSeconds(Math.Min(seconds, 300));
    }

    // ── source-generated log methods (CA1848 / CA1873) ───────────────────────

    [LoggerMessage(Level = LogLevel.Information,
        Message = "OutboxRelayService started. Instance={InstanceId} BatchSize={BatchSize} Interval={Interval}")]
    private static partial void LogStarted(
        ILogger logger, Guid instanceId, int batchSize, TimeSpan interval);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "OutboxRelayService stopped. Instance={InstanceId}")]
    private static partial void LogStopped(ILogger logger, Guid instanceId);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "OutboxRelayService batch loop threw unexpectedly. Sleeping before retry. Instance={InstanceId}")]
    private static partial void LogBatchLoopError(ILogger logger, Exception ex, Guid instanceId);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Claimed {Count} OutboxState rows. Instance={InstanceId}")]
    private static partial void LogClaimed(ILogger logger, int count, Guid instanceId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Relayed message. MessageId={MessageId} Type={Type} OutboxId={OutboxId}")]
    private static partial void LogRelayed(ILogger logger, Guid messageId, string type, Guid outboxId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Skipping expired message. MessageId={MessageId} OutboxId={OutboxId} ExpiredAt={Expiry}")]
    private static partial void LogSkippedExpired(ILogger logger, Guid messageId, Guid outboxId, DateTime expiry);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "OutboxId={OutboxId} exceeded MaxRetries={Max}. Moving to dead-letter.")]
    private static partial void LogDeadLettering(ILogger logger, Guid outboxId, int max);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "OutboxId={OutboxId} reached MaxRetries on this cycle. Moving to dead-letter immediately.")]
    private static partial void LogDeadLetteringOnExhaustion(ILogger logger, Guid outboxId);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Publish failed for OutboxId={OutboxId} Attempt={Attempts}/{Max}. Next retry in {Delay}.")]
    private static partial void LogPublishFailed(
        ILogger logger, Exception ex, Guid outboxId, int attempts, int max, TimeSpan delay);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Dead-lettered message. MessageId={MessageId} Type={Type} OutboxId={OutboxId} RetryCount={Retries}")]
    private static partial void LogDeadLettered(
        ILogger logger, Guid messageId, string type, Guid outboxId, int retries);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Unsupported ContentType={ContentType} for MessageId={MessageId}. Skipping (poison message).")]
    private static partial void LogPoisonContentType(ILogger logger, string contentType, Guid messageId);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Outbox Body missing 'message' property. MessageId={MessageId}. Skipping.")]
    private static partial void LogPoisonMissingMessage(ILogger logger, Guid messageId);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Cannot resolve .NET type for URN={Urn} MessageId={MessageId}. Skipping (poison message).")]
    private static partial void LogPoisonUnresolvableType(ILogger logger, string urn, Guid messageId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Failed to release own locks on shutdown. Instance={InstanceId}")]
    private static partial void LogReleaseOwnLocksFailed(ILogger logger, Exception ex, Guid instanceId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Released {Count} stale OutboxState locks on startup. Instance={InstanceId}")]
    private static partial void LogStaleLockReleased(ILogger logger, int count, Guid instanceId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Stale lock cleanup on startup failed. Instance={InstanceId}")]
    private static partial void LogStaleLockReleaseFailed(ILogger logger, Exception ex, Guid instanceId);
}
