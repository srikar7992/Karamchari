using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;
using MassTransit;
using Microsoft.AspNetCore.Http;

namespace Karamchari.Core.Multitenancy.Execution;

public sealed partial class TenantExecutionContextFactory
{
    [GeneratedRegex(TenantConstants.TenantIdPattern, RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex TenantIdRegex();

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

    public TenantExecutionEnvelope CreateFromConsumeContext(ConsumeContext consumeContext, string tenantId)
    {
        ArgumentNullException.ThrowIfNull(consumeContext);
        ValidateTenantId(tenantId);

        var correlationId = consumeContext.CorrelationId ?? Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var messageId = consumeContext.MessageId ?? Guid.NewGuid();
        var contentHash = ComputeContentHash(consumeContext);

        return new TenantExecutionEnvelope(
            tenantId,
            correlationId,
            requestId,
            ExecutionSource.MessageConsumer)
        {
            MessageId = messageId,
            ContentHash = contentHash
        };
    }

    public TenantExecutionEnvelope CreateForBackgroundJob(string tenantId, string jobId)
    {
        ValidateTenantId(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);

        var correlationId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

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
            ReplayMetadata = metadata
        };
    }

    public TenantExecutionEnvelope CreateForSaga(string tenantId, string sagaId, Guid correlationId)
    {
        ValidateTenantId(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sagaId);

        var requestId = Guid.NewGuid();

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
            ReplayMetadata = metadata
        };
    }

    public TenantExecutionEnvelope CreateForMigration(string tenantId, long version)
    {
        ValidateTenantId(tenantId);

        var correlationId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

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
            ReplayMetadata = metadata
        };
    }

    public TenantExecutionEnvelope CreateReplayContext(TenantExecutionEnvelope original, int attempt)
    {
        ArgumentNullException.ThrowIfNull(original);

        if (attempt < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attempt), "Attempt must be non-negative.");
        }

        var requestId = Guid.NewGuid();
        var metadata = new Dictionary<string, string>(original.ReplayMetadata)
        {
            { "OriginalRequestId", original.RequestId.ToString() },
            { "OriginalTimestamp", original.Timestamp.ToString("O", CultureInfo.InvariantCulture) },
            { "RetryAttempt", attempt.ToString(CultureInfo.InvariantCulture) }
        };

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

    private static Guid GetCorrelationId(HttpContext httpContext)
    {
        var correlationIdHeader = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(correlationIdHeader)
            && Guid.TryParse(correlationIdHeader, out var correlationId))
        {
            return correlationId;
        }

        return Guid.NewGuid();
    }

    private static Guid GetRequestId(HttpContext httpContext)
    {
        var requestIdHeader = httpContext.Request.Headers["X-Request-ID"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(requestIdHeader)
            && Guid.TryParse(requestIdHeader, out var requestId))
        {
            return requestId;
        }

        return Guid.NewGuid();
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
            var messageId = consumeContext.MessageId ?? Guid.NewGuid();
            var messageBytes = System.Text.Encoding.UTF8.GetBytes(messageId.ToString());
            return TenantExecutionEnvelope.ComputeContentHash(messageBytes);
        }
        catch
        {
            return null;
        }
    }
}
