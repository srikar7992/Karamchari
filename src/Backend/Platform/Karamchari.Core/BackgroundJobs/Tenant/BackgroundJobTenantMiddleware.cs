// -----------------------------------------------------------------------
// <copyright file="BackgroundJobTenantMiddleware.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Karamchari.Core.BackgroundJobs.Tenant;

public sealed partial class BackgroundJobTenantMiddleware
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

            LogRestoringTenantContext(_logger, envelope.TenantId, envelope.CorrelationId, envelope.RetryAttempt);

            scope = TenantJobExecutionScope.FromEnvelope(
                envelope,
                _serializer,
                NullTenantProvider.Instance,
                NullLogger<TenantJobExecutionScope>.Instance);

            await jobExecutor(envelope);

            LogJobCompleted(_logger, envelope.TenantId);
        }
        catch (StaleTenantRestoreException ex)
        {
            activity?.SetTag("tenant.error", "stale_context");
            activity?.SetStatus(ActivityStatusCode.Error, "Stale tenant context");

            LogStaleTenantContext(_logger, ex, ex.Message, ex.TenantId ?? "unknown");

            throw;
        }
        catch (Exception ex)
        {
            activity?.SetTag("tenant.error", "job_failed");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            if (envelope != null)
            {
                LogJobFailed(_logger, ex, envelope.TenantId, envelope.CorrelationId);
            }

            throw;
        }
        finally
        {
            scope?.Dispose();
            LogCleanupCompleted(_logger);
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

            LogRestoringTenantContextWithoutRetry(_logger, envelope.TenantId, envelope.CorrelationId);

            scope = TenantJobExecutionScope.FromEnvelope(
                envelope,
                _serializer,
                NullTenantProvider.Instance,
                NullLogger<TenantJobExecutionScope>.Instance);

            var result = await jobExecutor(envelope);

            LogJobCompleted(_logger, envelope.TenantId);

            return result;
        }
        catch (StaleTenantRestoreException ex)
        {
            activity?.SetTag("tenant.error", "stale_context");
            activity?.SetStatus(ActivityStatusCode.Error, "Stale tenant context");

            LogStaleTenantContextSimple(_logger, ex, ex.Message);

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

        LogValidatedTenantContext(_logger, envelope.TenantId, envelope.CorrelationId);

        return envelope;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Restoring tenant context for job: TenantId={TenantId}, CorrelationId={CorrelationId}, RetryAttempt={RetryAttempt}")]
    static partial void LogRestoringTenantContext(ILogger logger, string tenantId, string correlationId, int retryAttempt);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Job completed successfully for TenantId={TenantId}")]
    static partial void LogJobCompleted(ILogger logger, string tenantId);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Stale tenant context rejected for job: {Message}, TenantId={TenantId}")]
    static partial void LogStaleTenantContext(ILogger logger, Exception ex, string message, string tenantId);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Error,
        Message = "Job execution failed for TenantId={TenantId}, CorrelationId={CorrelationId}")]
    static partial void LogJobFailed(ILogger logger, Exception ex, string tenantId, string correlationId);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "Tenant context cleanup completed")]
    static partial void LogCleanupCompleted(ILogger logger);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Information,
        Message = "Restoring tenant context for job: TenantId={TenantId}, CorrelationId={CorrelationId}")]
    static partial void LogRestoringTenantContextWithoutRetry(ILogger logger, string tenantId, string correlationId);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Error,
        Message = "Stale tenant context rejected for job: {Message}")]
    static partial void LogStaleTenantContextSimple(ILogger logger, Exception ex, string message);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Debug,
        Message = "Validated tenant context for {TenantId}, CorrelationId={CorrelationId}")]
    static partial void LogValidatedTenantContext(ILogger logger, string tenantId, string correlationId);
}

internal sealed class NullTenantProvider : Karamchari.Core.Multitenancy.ITenantProvider
{
    public static NullTenantProvider Instance { get; } = new();

    private NullTenantProvider() { }

    public string GetCurrentTenantId()
    {
        throw new InvalidOperationException(
            "NullTenantProvider should not be used to resolve tenant in background job context.");
    }

    public bool TryGetCurrentTenantId([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? tenantId)
    {
        tenantId = null;
        return false;
    }

    public Karamchari.Core.Multitenancy.TenantExecutionEnvelope GetTenant()
    {
        throw new InvalidOperationException(
            "NullTenantProvider should not be used to resolve tenant in background job context.");
    }

    public bool TryGetTenant(out Karamchari.Core.Multitenancy.TenantExecutionEnvelope? tenant)
    {
        tenant = null;
        return false;
    }
}
