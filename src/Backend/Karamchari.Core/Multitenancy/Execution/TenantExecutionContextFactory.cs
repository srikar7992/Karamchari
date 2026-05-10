using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;
using MassTransit;
using Microsoft.AspNetCore.Http;

namespace Karamchari.Core.Multitenancy.Execution;

/// <summary>
/// Factory for creating <see cref="TenantExecutionEnvelope"/> and <see cref="TenantExecutionContext"/>
/// from various runtime sources like HTTP requests, message consumers, and background jobs.
/// </summary>
public sealed partial class TenantExecutionContextFactory
{
    [GeneratedRegex(TenantConstants.TenantIdPattern, RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex TenantIdRegex();

    /// <summary>
    /// Creates a tenant execution envelope from an HTTP context.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="tenantId">The validated tenant identifier.</param>
    /// <returns>A new tenant execution envelope.</returns>
    public TenantExecutionEnvelope CreateFromHttpContext(HttpContext httpContext, string tenantId)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ValidateTenantId(tenantId);

        var correlationId = GetCorrelationId(httpContext);
        var requestId = GetRequestId(httpContext);
        var traceId = GetTraceId();
        var spanId = GetSpanId();
        var userIdentity = GetUserIdentity(httpContext);

        return new TenantExecutionEnvelope(
            tenantId,
            correlationId,
            requestId,
            ExecutionSource.HttpRequest)
        {
            TraceId = traceId,
            SpanId = spanId,
            UserIdentity = userIdentity
        };
    }

    /// <summary>
    /// Creates a tenant execution envelope from a MassTransit consume context.
    /// </summary>
    /// <param name="consumeContext">The MassTransit consume context.</param>
    /// <param name="tenantId">The validated tenant identifier.</param>
    /// <returns>A new tenant execution envelope.</returns>
    public TenantExecutionEnvelope CreateFromConsumeContext(ConsumeContext consumeContext, string tenantId)
    {
        ArgumentNullException.ThrowIfNull(consumeContext);
        ValidateTenantId(tenantId);

        var correlationId = consumeContext.CorrelationId?.ToString() ?? Guid.NewGuid().ToString();
        var requestId = consumeContext.MessageId?.ToString() ?? Guid.NewGuid().ToString();
        var messageId = consumeContext.MessageId?.ToString();
        var contentHash = ComputeContentHash(consumeContext);

        return new TenantExecutionEnvelope(
            tenantId,
            correlationId,
            requestId,
            ExecutionSource.MessageConsumer)
        {
            MessageId = messageId,
            ContentHash = contentHash,
            TraceId = GetTraceId(),
            SpanId = GetSpanId()
        };
    }

    /// <summary>
    /// Creates a tenant execution envelope for a background job.
    /// </summary>
    /// <param name="tenantId">The validated tenant identifier.</param>
    /// <param name="jobId">The unique job identifier.</param>
    /// <returns>A new tenant execution envelope.</returns>
    public TenantExecutionEnvelope CreateForBackgroundJob(string tenantId, string jobId)
    {
        ValidateTenantId(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);

        var correlationId = Guid.NewGuid().ToString();
        var requestId = Guid.NewGuid().ToString();

        var metadata = new Dictionary<string, string>
        {
            { "JobId", jobId }
        };

        return new TenantExecutionEnvelope(
            tenantId,
            correlationId,
            requestId,
            ExecutionSource.BackgroundJob)
        {
            ReplayMetadata = metadata,
            TraceId = GetTraceId(),
            SpanId = GetSpanId()
        };
    }

    /// <summary>
    /// Creates a tenant execution envelope for a saga orchestration.
    /// </summary>
    /// <param name="tenantId">The validated tenant identifier.</param>
    /// <param name="sagaId">The unique saga identifier.</param>
    /// <param name="correlationId">The correlation ID associated with the saga.</param>
    /// <returns>A new tenant execution envelope.</returns>
    public TenantExecutionEnvelope CreateForSaga(string tenantId, string sagaId, string correlationId)
    {
        ValidateTenantId(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sagaId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        var requestId = Guid.NewGuid().ToString();

        var metadata = new Dictionary<string, string>
        {
            { "SagaId", sagaId }
        };

        return new TenantExecutionEnvelope(
            tenantId,
            correlationId,
            requestId,
            ExecutionSource.SagaOrchestration)
        {
            ReplayMetadata = metadata,
            TraceId = GetTraceId(),
            SpanId = GetSpanId()
        };
    }

    /// <summary>
    /// Creates a tenant execution envelope for a database migration.
    /// </summary>
    /// <param name="tenantId">The validated tenant identifier.</param>
    /// <param name="version">The migration version being applied.</param>
    /// <returns>A new tenant execution envelope.</returns>
    public TenantExecutionEnvelope CreateForMigration(string tenantId, long version)
    {
        ValidateTenantId(tenantId);

        var correlationId = Guid.NewGuid().ToString();
        var requestId = Guid.NewGuid().ToString();

        var metadata = new Dictionary<string, string>
        {
            { "MigrationVersion", version.ToString(CultureInfo.InvariantCulture) }
        };

        return new TenantExecutionEnvelope(
            tenantId,
            correlationId,
            requestId,
            ExecutionSource.Migration)
        {
            ReplayMetadata = metadata,
            TraceId = GetTraceId(),
            SpanId = GetSpanId()
        };
    }

    /// <summary>
    /// Creates a replay context from an original envelope, incrementing attempt counts.
    /// </summary>
    /// <param name="original">The original execution envelope.</param>
    /// <param name="attempt">The current retry/replay attempt number.</param>
    /// <returns>A new tenant execution envelope for the replay attempt.</returns>
    public TenantExecutionEnvelope CreateReplayContext(TenantExecutionEnvelope original, int attempt)
    {
        ArgumentNullException.ThrowIfNull(original);

        if (attempt < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attempt), "Attempt must be non-negative.");
        }

        var requestId = Guid.NewGuid().ToString();
        var metadata = new Dictionary<string, string>(original.ReplayMetadata);

        metadata["OriginalRequestId"] = original.RequestId;
        metadata["OriginalTimestamp"] = original.Timestamp.ToString("O", CultureInfo.InvariantCulture);
        metadata["RetryAttempt"] = attempt.ToString(CultureInfo.InvariantCulture);

        return original.With(
            RequestId: requestId,
            RetryAttempt: attempt,
            ReplayCount: original.ReplayCount + 1,
            ReplayMetadata: metadata);
    }

    private static void ValidateTenantId(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant id cannot be null or whitespace.", nameof(tenantId));
        }

        if (!TenantIdRegex().IsMatch(tenantId))
        {
            throw new ArgumentException(
                "Tenant id '" + tenantId + "' is invalid. Must match " + TenantConstants.TenantIdPattern + ".",
                nameof(tenantId));
        }
    }

    private static string GetCorrelationId(HttpContext httpContext)
    {
        var correlationIdHeader = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault();
        return !string.IsNullOrWhiteSpace(correlationIdHeader)
            ? correlationIdHeader
            : Guid.NewGuid().ToString();
    }

    private static string GetRequestId(HttpContext httpContext)
    {
        var requestIdHeader = httpContext.Request.Headers["X-Request-ID"].FirstOrDefault();
        return !string.IsNullOrWhiteSpace(requestIdHeader)
            ? requestIdHeader
            : Guid.NewGuid().ToString();
    }

    private static string? GetTraceId()
    {
        return Activity.Current?.TraceId.ToString();
    }

    private static string? GetSpanId()
    {
        return Activity.Current?.SpanId.ToString();
    }

    private static string? GetUserIdentity(HttpContext httpContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.User.FindFirst("sub")?.Value;
    }

    private static string? ComputeContentHash(ConsumeContext consumeContext)
    {
        try
        {
            var messageId = consumeContext.MessageId?.ToString() ?? Guid.NewGuid().ToString();
            var messageBytes = System.Text.Encoding.UTF8.GetBytes(messageId);
            return TenantExecutionEnvelope.ComputeContentHash(messageBytes);
        }
        catch
        {
            return null;
        }
    }
}
