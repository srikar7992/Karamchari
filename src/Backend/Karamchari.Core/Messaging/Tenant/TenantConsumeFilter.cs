using System.Diagnostics;
using System.Text.Json;
using Karamchari.Core.Multitenancy;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Messaging.Tenant;

public sealed class TenantConsumeFilter : IFilter<ConsumeContext>
{
    private readonly ILogger<TenantConsumeFilter> _logger;
    private readonly ReplayProtectionService _replayProtection;
    private readonly TimeSpan _staleMessageThreshold;

    public TenantConsumeFilter(
        ILogger<TenantConsumeFilter> logger,
        ReplayProtectionService replayProtection,
        TimeSpan? staleMessageThreshold = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(replayProtection);

        _logger = logger;
        _replayProtection = replayProtection;
        _staleMessageThreshold = staleMessageThreshold ?? TimeSpan.FromMinutes(5);
    }

    public async Task Send(ConsumeContext context, IPipe<ConsumeContext> next)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Processing message with ConversationId {ConversationId}",
                context.ConversationId);
        }

        try
        {
            context.Headers.TryGetHeader(TenantMessageHeaderKeys.TenantId, out var tenantIdObj);
            var tenantId = tenantIdObj as string;

            if (string.IsNullOrWhiteSpace(tenantId))
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(
                        "Message rejected: Missing tenant identifier in message, ConversationId {ConversationId}",
                        context.ConversationId);
                }

                throw new MalformedTenantMessageException(
                    "Message is missing required tenant identifier",
                    null, TenantMessageFailureReason.MissingTenantId);
            }

            if (!IsValidTenantIdFormat(tenantId))
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(
                        "Message rejected: Invalid tenant identifier format '{TenantId}' in message",
                        tenantId);
                }

                throw new MalformedTenantMessageException(
                    $"Tenant identifier '{tenantId}' has invalid format",
                    tenantId, TenantMessageFailureReason.InvalidTenantIdFormat);
            }

            context.Headers.TryGetHeader(TenantMessageHeaderKeys.Timestamp, out var timestampObj);
            if (!ValidateTimestamp(timestampObj))
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(
                        "Message rejected: Stale message detected for Tenant {TenantId}",
                        tenantId);
                }

                throw new MalformedTenantMessageException(
                    "Message timestamp is stale (older than 5 minutes)",
                    tenantId, TenantMessageFailureReason.StaleMessage);
            }

            context.Headers.TryGetHeader(TenantMessageHeaderKeys.OriginalMessageId, out var originalMessageIdObj);
            context.Headers.TryGetHeader(TenantMessageHeaderKeys.ContentHash, out var contentHashObj);
            var originalMessageId = originalMessageIdObj as string ?? context.MessageId?.ToString();
            var contentHash = contentHashObj as string;

            if (!string.IsNullOrWhiteSpace(originalMessageId) && _replayProtection.IsReplay(originalMessageId, contentHash))
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(
                        "Message rejected: Replay attack detected for Tenant {TenantId}, MessageId {MessageId}",
                        tenantId, originalMessageId);
                }

                throw new MalformedTenantMessageException(
                    "Message appears to be a replay and will be rejected",
                    tenantId, TenantMessageFailureReason.ReplayAttack);
            }

            var executionContext = TenantExecutionContext.CreateFromHeaders(null, context);
            executionContext?.SetAsCurrent();

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Tenant context validated for Tenant {TenantId}, CorrelationId {CorrelationId}",
                    tenantId, executionContext?.CorrelationId);
            }

            await next.Send(context);

            if (!string.IsNullOrWhiteSpace(originalMessageId))
            {
                _replayProtection.RecordProcessed(originalMessageId, contentHash);
            }
        }
        catch (MalformedTenantMessageException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not MalformedTenantMessageException)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex,
                    "Unexpected error processing tenant context for message");
            }

            throw new MalformedTenantMessageException(
                "Unexpected error during tenant context validation",
                null, TenantMessageFailureReason.MalformedEnvelope);
        }
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("tenant-consume-filter");
    }

    private static bool IsValidTenantIdFormat(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) return false;
        if (tenantId.Length > 50) return false;

        foreach (char c in tenantId)
        {
            if (!char.IsLetterOrDigit(c) && c != '-' && c != '_') return false;
        }

        return true;
    }

    private bool ValidateTimestamp(object? timestampObj)
    {
        DateTime? timestamp = null;

        if (timestampObj is DateTime dt)
        {
            timestamp = dt;
        }
        else if (timestampObj is string str && DateTime.TryParse(str, out var parsed))
        {
            timestamp = parsed;
        }

        if (!timestamp.HasValue)
        {
            return true;
        }

        var age = DateTime.UtcNow - timestamp.Value;
        return age <= _staleMessageThreshold;
    }
}

public static class TenantConsumeFilterExtensions
{
    public static void UseTenantConsumeFilter<T>(this IConsumerFactory<T> factory, IServiceProvider services)
        where T : class
    {
    }

    public static IServiceCollection AddTenantConsumeFilter(this IServiceCollection services, TimeSpan? staleMessageThreshold = null)
    {
        services.AddSingleton(sp =>
            new TenantConsumeFilter(
                sp.GetRequiredService<ILogger<TenantConsumeFilter>>(),
                sp.GetRequiredService<ReplayProtectionService>(),
                staleMessageThreshold));

        return services;
    }
}
