using System.Diagnostics;
using System.Globalization;
using Karamchari.Core.Multitenancy.Execution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Karamchari.Core.BackgroundJobs.Tenant;

public sealed class BackgroundJobTenantMiddleware
{
    private readonly ILogger<BackgroundJobTenantMiddleware> _logger;
    private readonly ITenantJobContextSerializer _serializer;
    private static readonly ActivitySource ActivitySource = new("Karamchari.BackgroundJobs");

    public BackgroundJobTenantMiddleware(
        ILogger<BackgroundJobTenantMiddleware> logger,
        ITenantJobContextSerializer serializer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    public async Task ExecuteAsync(
        string serializedContext,
        Func<TenantExecutionEnvelope, Task> jobExecutor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jobExecutor);

        using var activity = ActivitySource.StartActivity("BackgroundJobTenantMiddleware.Execute");
        activity?.SetTag("tenant.context.restored", true);

        TenantExecutionEnvelope? envelope = null;
        TenantJobExecutionScope? scope = null;

        try
        {
            envelope = _serializer.Deserialize(serializedContext);
            activity?.SetTag("tenant.id", envelope.TenantId);
            activity?.SetTag("tenant.correlation_id", envelope.CorrelationId.ToString());
            activity?.SetTag("tenant.retry_attempt", envelope.RetryAttempt);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Restoring tenant context for job: TenantId={TenantId}, CorrelationId={CorrelationId}, RetryAttempt={RetryAttempt}",
                    envelope.TenantId,
                    envelope.CorrelationId,
                    envelope.RetryAttempt);
            }

            scope = TenantJobExecutionScope.FromEnvelope(
                envelope,
                _serializer,
                NullTenantProvider.Instance,
                NullLogger<TenantJobExecutionScope>.Instance);

            await jobExecutor(envelope);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Job completed successfully for TenantId={TenantId}",
                    envelope.TenantId);
            }
        }
        catch (StaleTenantRestoreException ex)
        {
            activity?.SetTag("tenant.error", "stale_context");
            activity?.SetStatus(ActivityStatusCode.Error, "Stale tenant context");

            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(
                    ex,
                    "Stale tenant context rejected for job: {Message}, TenantId={TenantId}",
                    ex.Message,
                    ex.TenantId ?? "unknown");
            }

            throw;
        }
        catch (Exception ex)
        {
            activity?.SetTag("tenant.error", "job_failed");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            if (envelope != null && _logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(
                    ex,
                    "Job execution failed for TenantId={TenantId}, CorrelationId={CorrelationId}",
                    envelope.TenantId,
                    envelope.CorrelationId);
            }

            throw;
        }
        finally
        {
            scope?.Dispose();

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Tenant context cleanup completed");
            }
        }
    }

    public async Task<T> ExecuteAsync<T>(
        string serializedContext,
        Func<TenantExecutionEnvelope, Task<T>> jobExecutor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jobExecutor);

        using var activity = ActivitySource.StartActivity("BackgroundJobTenantMiddleware.Execute");
        activity?.SetTag("tenant.context.restored", true);

        TenantExecutionEnvelope? envelope = null;
        TenantJobExecutionScope? scope = null;

        try
        {
            envelope = _serializer.Deserialize(serializedContext);
            activity?.SetTag("tenant.id", envelope.TenantId);
            activity?.SetTag("tenant.correlation_id", envelope.CorrelationId.ToString());
            activity?.SetTag("tenant.retry_attempt", envelope.RetryAttempt);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Restoring tenant context for job: TenantId={TenantId}, CorrelationId={CorrelationId}",
                    envelope.TenantId,
                    envelope.CorrelationId);
            }

            scope = TenantJobExecutionScope.FromEnvelope(
                envelope,
                _serializer,
                NullTenantProvider.Instance,
                NullLogger<TenantJobExecutionScope>.Instance);

            var result = await jobExecutor(envelope);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Job completed successfully for TenantId={TenantId}",
                    envelope.TenantId);
            }

            return result;
        }
        catch (StaleTenantRestoreException ex)
        {
            activity?.SetTag("tenant.error", "stale_context");
            activity?.SetStatus(ActivityStatusCode.Error, "Stale tenant context");

            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(
                    ex,
                    "Stale tenant context rejected for job: {Message}",
                    ex.Message);
            }

            throw;
        }
        finally
        {
            scope?.Dispose();
        }
    }

    public TenantExecutionEnvelope ValidateAndRestore(string serializedContext)
    {
        if (string.IsNullOrWhiteSpace(serializedContext))
        {
            throw new StaleTenantRestoreException(
                "Cannot restore tenant context: serialized context is null or empty.");
        }

        if (_serializer.IsStale(serializedContext, TimeSpan.FromHours(24)))
        {
            throw new StaleTenantRestoreException(
                "Cannot restore tenant context: context is stale.");
        }

        var envelope = _serializer.Deserialize(serializedContext);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "Validated tenant context for {TenantId}, CorrelationId={CorrelationId}",
                envelope.TenantId,
                envelope.CorrelationId);
        }

        return envelope;
    }
}

internal sealed class NullTenantProvider : Karamchari.Core.Multitenancy.ITenantProvider
{
    public static NullTenantProvider Instance { get; } = new();

    private NullTenantProvider() { }

    public Karamchari.Core.Multitenancy.TenantContext GetTenant()
    {
        throw new InvalidOperationException(
            "NullTenantProvider should not be used to resolve tenant in background job context.");
    }

    public bool TryGetTenant(out Karamchari.Core.Multitenancy.TenantContext? tenant)
    {
        tenant = null;
        return false;
    }
}
