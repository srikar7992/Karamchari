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
/// Production outbox relay: polls <c>dbo.OutboxMessage</c> in batches, publishes
/// each message to the configured MassTransit transport (Azure Service Bus),
/// and marks delivery only after a successful atomic commit.
///
/// Correctness guarantees:
/// <list type="bullet">
///   <item>At-least-once delivery with idempotent consumer requirement.</item>
///   <item>Crash safety: retry state persisted in <c>dbo.OutboxProcessingState</c>; restarts resume progress.</item>
///   <item>Multi-instance safety: SQL Server UPDLOCK + READPAST + ROWLOCK prevents double-claiming.</item>
///   <item>Stale lock recovery: uses <c>LockAcquiredUtc</c> for time-based cleanup, not row creation time.</item>
///   <item>Atomic delivery completion: DELETE + UPDATE + DELETE in a single SQL transaction.</item>
///   <item>Poison isolation: structural defects dead-letter immediately; transient failures retry with backoff.</item>
///   <item>Circuit breaker: sustained Service Bus failures open the circuit to prevent DB hammering.</item>
///   <item>Each concurrent task uses an isolated SQL connection from the pool.</item>
/// </list>
/// </summary>
public sealed partial class OutboxRelayService : BackgroundService
{
    // â”€â”€ row representation â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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

    // â”€â”€ poison isolation: throw this to dead-letter without retry â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private sealed class PoisonMessageException : Exception
    {
        internal PoisonMessageException(string reason) : base(reason) { }
    }

    // â”€â”€ full-envelope publish pipe â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private sealed class PublishEnvelopePipe : IPipe<PublishContext>
    {
        private readonly OutboxMessageRow _row;
        private readonly string _envelopeJson;

        internal PublishEnvelopePipe(OutboxMessageRow row, string envelopeJson)
        {
            _row = row;
            _envelopeJson = envelopeJson;
        }

        /// <inheritdoc />
        public Task Send(PublishContext context)
        {
            ApplyCommonContext(context, _row);
            ApplyEnvelopeExtras(context.Headers, context, _envelopeJson);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Probe(ProbeContext context) { }
    }

    private sealed class SendEnvelopePipe : IPipe<SendContext>
    {
        private readonly OutboxMessageRow _row;
        private readonly string _envelopeJson;

        internal SendEnvelopePipe(OutboxMessageRow row, string envelopeJson)
        {
            _row = row;
            _envelopeJson = envelopeJson;
        }

        /// <inheritdoc />
        public Task Send(SendContext context)
        {
            ApplyCommonContext(context, _row);
            ApplyEnvelopeExtras(context.Headers, context, _envelopeJson);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Probe(ProbeContext context) { }
    }

    // â”€â”€ fields â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IBus _bus;
    private readonly OutboxRelayMetrics _metrics;
    private readonly IOptions<OutboxRelayOptions> _options;
    private readonly ILogger<OutboxRelayService> _logger;
    private readonly OutboxMessageTypeRegistry _typeRegistry;
    private readonly OutboxRelayCircuitBreaker _circuitBreaker;

    /// <summary>Stable per-process GUID used as the claim token in <c>dbo.OutboxState.LockId</c>.</summary>
    private readonly Guid _instanceId = Guid.NewGuid();

    private int _gaugeUpdateTick;

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Initializes a new instance of <see cref="OutboxRelayService"/>.
    /// </summary>
    public OutboxRelayService(
        IServiceScopeFactory scopeFactory,
        IBus bus,
        OutboxRelayMetrics metrics,
        IOptions<OutboxRelayOptions> options,
        ILogger<OutboxRelayService> logger,
        OutboxMessageTypeRegistry typeRegistry,
        OutboxRelayCircuitBreaker circuitBreaker)
    {
        _scopeFactory = scopeFactory;
        _bus = bus;
        _metrics = metrics;
        _options = options;
        _logger = logger;
        _typeRegistry = typeRegistry;
        _circuitBreaker = circuitBreaker;
    }

    // â”€â”€ main loop â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = _options.Value;
        LogStarted(_logger, _instanceId, opts.BatchSize, opts.PollingInterval);

        // Release stale locks from prior crashed instances before claiming anything.
        await TryReleaseStaleLockAsync(CancellationToken.None);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_circuitBreaker.ShouldSkip)
            {
                LogCircuitOpen(_logger, _instanceId, _circuitBreaker.ConsecutiveFailures);
                try { await Task.Delay(opts.CircuitBreakerCooldownInterval, stoppingToken); }
                catch (OperationCanceledException) { break; }
                continue;
            }

            var sw = Stopwatch.StartNew();
            try
            {
                var processed = await ProcessBatchAsync(stoppingToken);
                _metrics.BatchCycles.Add(1);
                _metrics.BatchDurationMs.Record(sw.Elapsed.TotalMilliseconds);

                if (processed == 0)
                    await Task.Delay(opts.PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogBatchLoopError(_logger, ex, _instanceId);
                _metrics.BatchDurationMs.Record(sw.Elapsed.TotalMilliseconds);
                try { await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }

        await TryReleaseOwnLocksAsync(CancellationToken.None);
        LogStopped(_logger, _instanceId);
    }

    // â”€â”€ batch orchestration â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private async Task<int> ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OutboxRelayDbContext>();

        await using var connection = (SqlConnection)db.Database.GetDbConnection();
        await connection.OpenAsync(ct);

        // 1. Claim OutboxState rows atomically.
        var claimed = await ClaimBatchAsync(connection, ct);
        if (claimed.Count == 0) return 0;

        // 2. Persist lock metadata (LockAcquiredUtc, LockedByInstanceId) for stale detection.
        await UpsertProcessingStateLockAsync(connection, claimed, ct);

        LogClaimed(_logger, claimed.Count, _instanceId);

        // 3. Load associated OutboxMessage rows for claimed IDs.
        var messages = await LoadMessagesAsync(connection, claimed, ct);

        // 4. Release claim for IDs with no messages (already delivered by another instance).
        var toRelease = claimed.Except(messages.Keys).ToList();
        if (toRelease.Count > 0)
            await ReleaseLocksAsync(connection, toRelease, ct);

        if (messages.Count == 0) return 0;

        // 5. Process each OutboxId concurrently; each task uses its own isolated connection.
        var opts = _options.Value;
        var semaphore = new SemaphoreSlim(opts.MaxConcurrency);
        var tasks = messages.Select(async kv =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                await using var taskScope = _scopeFactory.CreateAsyncScope();
                var taskDb = taskScope.ServiceProvider.GetRequiredService<OutboxRelayDbContext>();
                await using var taskConn = (SqlConnection)taskDb.Database.GetDbConnection();
                await taskConn.OpenAsync(ct);
                await ProcessOutboxIdAsync(taskConn, kv.Key, kv.Value, ct);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        // 6. Periodically update observable gauges.
        _gaugeUpdateTick++;
        if (_gaugeUpdateTick % opts.GaugeUpdateCycleInterval == 0)
            await TryUpdateQueueGaugesAsync(connection, ct);

        return messages.Count;
    }

    // â”€â”€ per-outbox-id processing â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private async Task ProcessOutboxIdAsync(
        SqlConnection connection,
        Guid outboxId,
        List<OutboxMessageRow> messages,
        CancellationToken ct)
    {
        var opts = _options.Value;

        // Load persisted retry state â€” null means first attempt.
        var state = await LoadProcessingStateAsync(connection, outboxId, ct);

        // Honour backoff window â€” another instance or the same instance on restart set NextRetryUtc.
        if (state?.NextRetryUtc.HasValue == true && DateTime.UtcNow < state.NextRetryUtc.Value)
        {
            await ReleaseLocksAsync(connection, [outboxId], ct);
            return;
        }

        // Defensive: should have been dead-lettered already, but guard against state divergence.
        if (state != null && state.RetryCount >= opts.MaxRetries)
        {
            LogDeadLettering(_logger, outboxId, opts.MaxRetries);
            await DeadLetterBatchAsync(connection, outboxId, messages,
                state.RetryCount, state.LastError ?? "Exceeded MaxRetries", CancellationToken.None);
            return;
        }

        var publishSw = Stopwatch.StartNew();
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
                _metrics.PublishLatencyMs.Record(publishSw.Elapsed.TotalMilliseconds);
                publishSw.Restart();

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    var shortType = FirstTypeShortName(msg.MessageType);
                    LogRelayed(_logger, msg.MessageId, shortType, outboxId);
                }
            }

            // Atomic: delete messages + mark delivered + delete processing state.
            await MarkDeliveredAtomicAsync(connection, outboxId, ct);
            _circuitBreaker.RecordSuccess();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Graceful shutdown: release the lock so another instance can pick up.
            await ReleaseLocksAsync(connection, [outboxId], CancellationToken.None);
            throw;
        }
        catch (PoisonMessageException pmEx)
        {
            // Structural defect: dead-letter immediately without retrying.
            _metrics.PoisonMessages.Add(1);
            LogPoisonDeadLettering(_logger, outboxId, pmEx.Message);
            await DeadLetterBatchAsync(connection, outboxId, messages,
                state?.RetryCount ?? 0, pmEx.Message, CancellationToken.None);
        }
        catch (Exception ex)
        {
            var attempts = (state?.RetryCount ?? 0) + 1;
            var delay = ComputeRetryDelay(attempts, opts);

            _metrics.MessagesFailed.Add(1);
            _circuitBreaker.RecordFailure();
            LogPublishFailed(_logger, ex, outboxId, attempts, opts.MaxRetries, delay);

            var nextRetry = DateTime.UtcNow + delay;
            await PersistRetryStateAsync(
                connection, outboxId, attempts, nextRetry, TruncateError(ex.Message), CancellationToken.None);
            // PersistRetryStateAsync also nulls LockedByInstanceId + LockAcquiredUtc in ProcessingState.
            // Still need to release the OutboxState.LockId.
            await ReleaseLocksAsync(connection, [outboxId], CancellationToken.None);

            if (attempts >= opts.MaxRetries)
            {
                LogDeadLetteringOnExhaustion(_logger, outboxId);
                await DeadLetterBatchAsync(connection, outboxId, messages,
                    attempts, ex.Message, CancellationToken.None);
            }
        }
    }

    // â”€â”€ publish â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private async Task PublishMessageAsync(OutboxMessageRow msg, CancellationToken ct)
    {
        // Validate content type â€” reject anything that is neither MassTransit JSON nor plain JSON.
        if (msg.ContentType != null
            && !msg.ContentType.StartsWith("application/vnd.masstransit+json", StringComparison.OrdinalIgnoreCase)
            && !msg.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
        {
            throw new PoisonMessageException($"Unsupported ContentType='{msg.ContentType}' MessageId={msg.MessageId}");
        }

        // Parse the MassTransit JSON envelope that was stored verbatim in OutboxMessage.Body.
        using var doc = JsonDocument.Parse(msg.Body);
        var root = doc.RootElement;

        if (!root.TryGetProperty("message", out var payloadElement))
            throw new PoisonMessageException($"Body missing 'message' property. MessageId={msg.MessageId}");

        var resolvedType = _typeRegistry.Resolve(msg.MessageType);
        if (resolvedType is null)
        {
            var primaryUrn = msg.MessageType.Split(';', StringSplitOptions.TrimEntries)[0];
            throw new PoisonMessageException($"Cannot resolve CLR type for URN='{primaryUrn}' MessageId={msg.MessageId}");
        }

        var payload = payloadElement.Deserialize(resolvedType, _jsonOptions)
            ?? throw new PoisonMessageException(
                $"Deserialised null for type '{resolvedType.FullName}' MessageId={msg.MessageId}");

        // Capture the raw envelope JSON before doc is disposed â€” pipes need it for header restoration.
        var envelopeJson = msg.Body;

        if (!string.IsNullOrEmpty(msg.DestinationAddress)
            && Uri.TryCreate(msg.DestinationAddress, UriKind.Absolute, out var destUri))
        {
            var endpoint = await _bus.GetSendEndpoint(destUri);
            await endpoint.Send(payload, resolvedType, new SendEnvelopePipe(msg, envelopeJson), ct);
        }
        else
        {
            await _bus.Publish(payload, resolvedType, new PublishEnvelopePipe(msg, envelopeJson), ct);
        }
    }

    // â”€â”€ context helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    // SendContext is the base for both PublishContext and standalone send â€” all writable ID fields live here.
    private static void ApplyCommonContext(SendContext ctx, OutboxMessageRow row)
    {
        ctx.MessageId = row.MessageId;
        if (row.CorrelationId.HasValue) ctx.CorrelationId = row.CorrelationId;
        if (row.ConversationId.HasValue) ctx.ConversationId = row.ConversationId;
        if (row.InitiatorId.HasValue) ctx.InitiatorId = row.InitiatorId;
        if (row.RequestId.HasValue) ctx.RequestId = row.RequestId;

        // Preserve TTL only if the message has not yet expired.
        if (row.ExpirationTime.HasValue && row.ExpirationTime.Value > DateTime.UtcNow)
            ctx.TimeToLive = row.ExpirationTime.Value - DateTime.UtcNow;
    }

    private static void ApplyEnvelopeExtras(SendHeaders headers, SendContext ctx, string envelopeJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(envelopeJson);
            var root = doc.RootElement;

            // Restore custom application headers stored in the "headers" object.
            if (root.TryGetProperty("headers", out var headersEl)
                && headersEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in headersEl.EnumerateObject())
                {
                    var value = prop.Value.ValueKind == JsonValueKind.String
                        ? prop.Value.GetString()
                        : prop.Value.GetRawText();
                    headers.Set(prop.Name, value);
                }
            }

            // Restore response/fault address overrides if present.
            if (ctx is PublishContext pctx)
            {
                if (root.TryGetProperty("responseAddress", out var respEl)
                    && respEl.ValueKind == JsonValueKind.String
                    && Uri.TryCreate(respEl.GetString(), UriKind.Absolute, out var respUri))
                    pctx.ResponseAddress = respUri;

                if (root.TryGetProperty("faultAddress", out var faultEl)
                    && faultEl.ValueKind == JsonValueKind.String
                    && Uri.TryCreate(faultEl.GetString(), UriKind.Absolute, out var faultUri))
                    pctx.FaultAddress = faultUri;
            }
        }
        catch (JsonException) { /* best-effort header restoration; envelope may be non-standard */ }
    }

    // â”€â”€ claiming â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private async Task<List<Guid>> ClaimBatchAsync(SqlConnection connection, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.OutboxState
            SET    LockId = @lockId
            OUTPUT INSERTED.OutboxId
            WHERE  OutboxId IN (
                SELECT TOP (@batchSize) OutboxId
                FROM   dbo.OutboxState WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE  LockId    IS NULL
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

    // â”€â”€ processing state persistence â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private async Task UpsertProcessingStateLockAsync(
        SqlConnection connection, List<Guid> outboxIds, CancellationToken ct)
    {
        const string sql = """
            MERGE dbo.OutboxProcessingState AS target
            USING (SELECT @outboxId AS OutboxId) AS source ON target.OutboxId = source.OutboxId
            WHEN MATCHED THEN
                UPDATE SET LockedByInstanceId = @instanceId,
                           LockAcquiredUtc    = SYSUTCDATETIME()
            WHEN NOT MATCHED THEN
                INSERT (OutboxId, RetryCount, LockedByInstanceId, LockAcquiredUtc)
                VALUES (@outboxId, 0, @instanceId, SYSUTCDATETIME());
            """;

        foreach (var outboxId in outboxIds)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new SqlParameter("@outboxId", SqlDbType.UniqueIdentifier) { Value = outboxId });
            cmd.Parameters.Add(new SqlParameter("@instanceId", SqlDbType.UniqueIdentifier) { Value = _instanceId });
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }

    private static async Task<OutboxProcessingState?> LoadProcessingStateAsync(
        SqlConnection connection, Guid outboxId, CancellationToken ct)
    {
        const string sql = """
            SELECT RetryCount, LastAttemptUtc, NextRetryUtc, LastError, LockedByInstanceId, LockAcquiredUtc
            FROM   dbo.OutboxProcessingState
            WHERE  OutboxId = @outboxId
            """;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@outboxId", SqlDbType.UniqueIdentifier) { Value = outboxId });

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return new OutboxProcessingState
        {
            OutboxId = outboxId,
            RetryCount = reader.GetInt32(0),
            LastAttemptUtc = reader.IsDBNull(1) ? null : reader.GetDateTime(1),
            NextRetryUtc = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
            LastError = reader.IsDBNull(3) ? null : reader.GetString(3),
            LockedByInstanceId = reader.IsDBNull(4) ? null : reader.GetGuid(4),
            LockAcquiredUtc = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
        };
    }

    private static async Task PersistRetryStateAsync(
        SqlConnection connection,
        Guid outboxId,
        int retryCount,
        DateTime nextRetryUtc,
        string? lastError,
        CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.OutboxProcessingState
            SET    RetryCount         = @retryCount,
                   LastAttemptUtc     = SYSUTCDATETIME(),
                   NextRetryUtc       = @nextRetry,
                   LastError          = @lastError,
                   LockedByInstanceId = NULL,
                   LockAcquiredUtc    = NULL
            WHERE  OutboxId = @outboxId
            """;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@outboxId", SqlDbType.UniqueIdentifier) { Value = outboxId });
        cmd.Parameters.Add(new SqlParameter("@retryCount", SqlDbType.Int) { Value = retryCount });
        cmd.Parameters.Add(new SqlParameter("@nextRetry", SqlDbType.DateTime2) { Value = nextRetryUtc });
        cmd.Parameters.Add(new SqlParameter("@lastError", SqlDbType.NVarChar) { Size = 2000, Value = (object?)lastError ?? DBNull.Value });
        await cmd.ExecuteNonQueryAsync(ct);
    }

    // â”€â”€ message loading â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static async Task<Dictionary<Guid, List<OutboxMessageRow>>> LoadMessagesAsync(
        SqlConnection connection, List<Guid> outboxIds, CancellationToken ct)
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
                SequenceNumber: reader.GetInt64(0),
                MessageId: reader.GetGuid(1),
                OutboxId: reader.GetGuid(2),
                Body: reader.GetString(3),
                MessageType: reader.GetString(4),
                DestinationAddress: reader.IsDBNull(5) ? null : reader.GetString(5),
                Headers: reader.IsDBNull(6) ? null : reader.GetString(6),
                SentTime: reader.GetDateTime(7),
                ExpirationTime: reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                ContentType: reader.IsDBNull(9) ? null : reader.GetString(9),
                CorrelationId: reader.IsDBNull(10) ? null : reader.GetGuid(10),
                ConversationId: reader.IsDBNull(11) ? null : reader.GetGuid(11),
                InitiatorId: reader.IsDBNull(12) ? null : reader.GetGuid(12),
                RequestId: reader.IsDBNull(13) ? null : reader.GetGuid(13));

            if (!result.TryGetValue(row.OutboxId, out var list))
            {
                list = [];
                result[row.OutboxId] = list;
            }
            list.Add(row);
        }

        return result;
    }

    // â”€â”€ atomic delivery completion â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static async Task MarkDeliveredAtomicAsync(
        SqlConnection connection, Guid outboxId, CancellationToken ct)
    {
        await using var tx = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct) as SqlTransaction
            ?? throw new InvalidOperationException("Expected SqlTransaction from BeginTransactionAsync.");
        try
        {
            await using var del = connection.CreateCommand();
            del.Transaction = tx;
            del.CommandText = "DELETE FROM dbo.OutboxMessage WHERE OutboxId = @id";
            del.Parameters.Add(new SqlParameter("@id", SqlDbType.UniqueIdentifier) { Value = outboxId });
            await del.ExecuteNonQueryAsync(ct);

            await using var upd = connection.CreateCommand();
            upd.Transaction = tx;
            upd.CommandText = """
                UPDATE dbo.OutboxState
                SET    Delivered = SYSUTCDATETIME(),
                       LockId    = NULL
                WHERE  OutboxId = @id
                """;
            upd.Parameters.Add(new SqlParameter("@id", SqlDbType.UniqueIdentifier) { Value = outboxId });
            await upd.ExecuteNonQueryAsync(ct);

            await using var delState = connection.CreateCommand();
            delState.Transaction = tx;
            delState.CommandText = "DELETE FROM dbo.OutboxProcessingState WHERE OutboxId = @id";
            delState.Parameters.Add(new SqlParameter("@id", SqlDbType.UniqueIdentifier) { Value = outboxId });
            await delState.ExecuteNonQueryAsync(ct);

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    // â”€â”€ dead-lettering â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private async Task DeadLetterBatchAsync(
        SqlConnection connection,
        Guid outboxId,
        List<OutboxMessageRow> messages,
        int retryCount,
        string failureReason,
        CancellationToken ct)
    {
        await using var tx = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct) as SqlTransaction
            ?? throw new InvalidOperationException("Expected SqlTransaction from BeginTransactionAsync.");
        try
        {
            foreach (var msg in messages)
            {
                await InsertDeadLetterAsync(connection, tx, msg, outboxId, retryCount,
                    TruncateError(failureReason), ct);

                _metrics.MessagesDeadLettered.Add(1);
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    var shortType = FirstTypeShortName(msg.MessageType);
                    LogDeadLettered(_logger, msg.MessageId, shortType, outboxId, retryCount);
                }
            }

            // Same atomic operation as successful delivery: clean up outbox tables.
            await using var del = connection.CreateCommand();
            del.Transaction = tx;
            del.CommandText = "DELETE FROM dbo.OutboxMessage WHERE OutboxId = @id";
            del.Parameters.Add(new SqlParameter("@id", SqlDbType.UniqueIdentifier) { Value = outboxId });
            await del.ExecuteNonQueryAsync(ct);

            await using var upd = connection.CreateCommand();
            upd.Transaction = tx;
            upd.CommandText = """
                UPDATE dbo.OutboxState
                SET    Delivered = SYSUTCDATETIME(),
                       LockId    = NULL
                WHERE  OutboxId = @id
                """;
            upd.Parameters.Add(new SqlParameter("@id", SqlDbType.UniqueIdentifier) { Value = outboxId });
            await upd.ExecuteNonQueryAsync(ct);

            await using var delState = connection.CreateCommand();
            delState.Transaction = tx;
            delState.CommandText = "DELETE FROM dbo.OutboxProcessingState WHERE OutboxId = @id";
            delState.Parameters.Add(new SqlParameter("@id", SqlDbType.UniqueIdentifier) { Value = outboxId });
            await delState.ExecuteNonQueryAsync(ct);

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static async Task InsertDeadLetterAsync(
        SqlConnection connection,
        SqlTransaction tx,
        OutboxMessageRow msg,
        Guid outboxId,
        int retryCount,
        string failureReason,
        CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.OutboxDeadLetter
                (MessageId, OutboxId, MessageType, Body, Headers, DestinationAddress, FailureReason, RetryCount, FailedAt)
            VALUES
                (@messageId, @outboxId, @messageType, @body, @headers, @destinationAddress, @failureReason, @retryCount, SYSUTCDATETIME())
            """;

        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@messageId", SqlDbType.UniqueIdentifier) { Value = msg.MessageId });
        cmd.Parameters.Add(new SqlParameter("@outboxId", SqlDbType.UniqueIdentifier) { Value = outboxId });
        cmd.Parameters.Add(new SqlParameter("@messageType", SqlDbType.NVarChar) { Size = 500, Value = msg.MessageType });
        cmd.Parameters.Add(new SqlParameter("@body", SqlDbType.NVarChar) { Size = -1, Value = msg.Body });
        cmd.Parameters.Add(new SqlParameter("@headers", SqlDbType.NVarChar) { Size = -1, Value = (object?)msg.Headers ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@destinationAddress", SqlDbType.NVarChar) { Size = 500, Value = (object?)msg.DestinationAddress ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@failureReason", SqlDbType.NVarChar) { Size = 2000, Value = failureReason });
        cmd.Parameters.Add(new SqlParameter("@retryCount", SqlDbType.Int) { Value = retryCount });
        await cmd.ExecuteNonQueryAsync(ct);
    }

    // â”€â”€ lock management â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private async Task ReleaseLocksAsync(
        SqlConnection connection, List<Guid> outboxIds, CancellationToken ct)
    {
        if (outboxIds.Count == 0) return;

        var paramNames = outboxIds.Select((_, i) => $"@id{i}").ToArray();
        var sql = $"""
            UPDATE dbo.OutboxState
            SET    LockId = NULL
            WHERE  LockId   = @lockId
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

            await using var cmdState = connection.CreateCommand();
            cmdState.CommandText = """
                UPDATE dbo.OutboxProcessingState
                SET    LockedByInstanceId = NULL,
                       LockAcquiredUtc    = NULL
                WHERE  LockedByInstanceId = @instanceId
                """;
            cmdState.Parameters.Add(new SqlParameter("@instanceId", SqlDbType.UniqueIdentifier) { Value = _instanceId });
            await cmdState.ExecuteNonQueryAsync(ct);
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

            // Release OutboxState locks for rows whose processing state shows a stale LockAcquiredUtc.
            // Uses LockAcquiredUtc (accurate lock timestamp) â€” NOT OutboxState.Created (wrong for stale detection).
            await using var releaseLocks = connection.CreateCommand();
            releaseLocks.CommandText = """
                UPDATE os
                SET    os.LockId = NULL
                FROM   dbo.OutboxState os
                JOIN   dbo.OutboxProcessingState ps ON ps.OutboxId = os.OutboxId
                WHERE  ps.LockedByInstanceId IS NOT NULL
                  AND  ps.LockAcquiredUtc    < DATEADD(SECOND, @negStaleSecs, SYSUTCDATETIME())
                  AND  os.Delivered          IS NULL
                """;
            releaseLocks.Parameters.Add(new SqlParameter("@negStaleSecs", SqlDbType.Int)
            {
                Value = -(int)_options.Value.StaleLockTimeout.TotalSeconds
            });
            var releasedLocks = await releaseLocks.ExecuteNonQueryAsync(ct);

            // Clear the corresponding processing state lock metadata.
            await using var clearState = connection.CreateCommand();
            clearState.CommandText = """
                UPDATE dbo.OutboxProcessingState
                SET    LockedByInstanceId = NULL,
                       LockAcquiredUtc    = NULL
                WHERE  LockedByInstanceId IS NOT NULL
                  AND  LockAcquiredUtc    < DATEADD(SECOND, @negStaleSecs, SYSUTCDATETIME())
                """;
            clearState.Parameters.Add(new SqlParameter("@negStaleSecs", SqlDbType.Int)
            {
                Value = -(int)_options.Value.StaleLockTimeout.TotalSeconds
            });
            await clearState.ExecuteNonQueryAsync(ct);

            if (releasedLocks > 0)
            {
                _metrics.StaleLockRecoveries.Add(releasedLocks);
                LogStaleLockReleased(_logger, releasedLocks, _instanceId);
            }
        }
        catch (Exception ex)
        {
            LogStaleLockReleaseFailed(_logger, ex, _instanceId);
        }
    }

    // â”€â”€ observable gauge updates â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private async Task TryUpdateQueueGaugesAsync(SqlConnection connection, CancellationToken ct)
    {
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                SELECT
                    (SELECT COUNT(*)  FROM dbo.OutboxState          WHERE Delivered IS NULL)          AS QueueDepth,
                    (SELECT DATEDIFF(SECOND, MIN(Created), SYSUTCDATETIME())
                             FROM dbo.OutboxState WHERE Delivered IS NULL)                            AS OldestAgeSeconds,
                    (SELECT COUNT(*)  FROM dbo.OutboxProcessingState WHERE RetryCount > 0)            AS RetryBacklog,
                    (SELECT COUNT(*)  FROM dbo.OutboxDeadLetter)                                      AS DlqSize
                """;

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                _metrics.SetQueueDepth(reader.IsDBNull(0) ? 0 : reader.GetInt32(0));
                _metrics.SetOldestPendingAgeSeconds(reader.IsDBNull(1) ? 0 : reader.GetInt32(1));
                _metrics.SetRetryBacklog(reader.IsDBNull(2) ? 0 : reader.GetInt32(2));
                _metrics.SetDlqSize(reader.IsDBNull(3) ? 0 : reader.GetInt32(3));
            }
        }
        catch (Exception ex)
        {
            LogGaugeUpdateFailed(_logger, ex, _instanceId);
        }
    }

    // â”€â”€ helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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

    private static string TruncateError(string? error) =>
        error is null ? string.Empty
        : error.Length > 2000 ? error[..2000]
        : error;

    // â”€â”€ source-generated log methods (CA1848 / CA1873) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [LoggerMessage(Level = LogLevel.Information,
        Message = "OutboxRelayService started. Instance={InstanceId} BatchSize={BatchSize} Interval={Interval}")]
    private static partial void LogStarted(ILogger logger, Guid instanceId, int batchSize, TimeSpan interval);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "OutboxRelayService stopped. Instance={InstanceId}")]
    private static partial void LogStopped(ILogger logger, Guid instanceId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Circuit breaker open. Skipping batch cycle. Instance={InstanceId} ConsecutiveFailures={Failures}")]
    private static partial void LogCircuitOpen(ILogger logger, Guid instanceId, int failures);

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

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Poison message dead-lettered immediately. OutboxId={OutboxId} Reason={Reason}")]
    private static partial void LogPoisonDeadLettering(ILogger logger, Guid outboxId, string reason);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Publish failed for OutboxId={OutboxId} Attempt={Attempts}/{Max}. Next retry in {Delay}.")]
    private static partial void LogPublishFailed(
        ILogger logger, Exception ex, Guid outboxId, int attempts, int max, TimeSpan delay);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Dead-lettered message. MessageId={MessageId} Type={Type} OutboxId={OutboxId} RetryCount={Retries}")]
    private static partial void LogDeadLettered(
        ILogger logger, Guid messageId, string type, Guid outboxId, int retries);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Failed to release own locks on shutdown. Instance={InstanceId}")]
    private static partial void LogReleaseOwnLocksFailed(ILogger logger, Exception ex, Guid instanceId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Released {Count} stale OutboxState locks on startup. Instance={InstanceId}")]
    private static partial void LogStaleLockReleased(ILogger logger, int count, Guid instanceId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Stale lock cleanup on startup failed. Instance={InstanceId}")]
    private static partial void LogStaleLockReleaseFailed(ILogger logger, Exception ex, Guid instanceId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Queue gauge update failed. Instance={InstanceId}")]
    private static partial void LogGaugeUpdateFailed(ILogger logger, Exception ex, Guid instanceId);
}
