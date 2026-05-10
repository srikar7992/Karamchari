using System.Globalization;
using System.Text.RegularExpressions;
using Karamchari.Core.Multitenancy;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Persistence.Filters;

public sealed class QueryFilterBypassDetector
{
    private static readonly Regex BypassPatterns = new(
        @"(?i)\b(ignorequeryfilters|ignoreallqueryfilters|asnotracking|no\btracking)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<QueryFilterBypassDetector> _logger;

    public event EventHandler<QueryFilterBypassEventArgs>? BypassDetected;

    public QueryFilterBypassDetector(
        ITenantProvider tenantProvider,
        ILogger<QueryFilterBypassDetector> logger)
    {
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void DetectBypassAttempt(string methodName, string? contextInfo = null)
    {
        if (string.IsNullOrWhiteSpace(methodName))
        {
            return;
        }

        var tenantId = _tenantProvider.TryGetTenant(out var tenant) ? tenant?.TenantId : "Unknown";

        var bypassInfo = new QueryFilterBypassInfo
        {
            MethodName = methodName,
            DetectedAt = DateTime.UtcNow,
            TenantId = tenantId ?? "NoContext",
            StackTrace = Environment.StackTrace,
            ContextInfo = contextInfo
        };

        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning(
                "SECURITY ALERT: Query filter bypass detected!{Newline}" +
                "Method: {MethodName}{Newline}" +
                "TenantId: {TenantId}{Newline}" +
                "Context: {ContextInfo}{Newline}" +
                "Stack Trace:{Newline}{StackTrace}",
                Environment.NewLine,
                methodName,
                tenantId ?? "Unknown",
                contextInfo ?? "N/A",
                Environment.NewLine,
                Environment.StackTrace);
        }

        TriggerAlert(bypassInfo);

        BypassDetected?.Invoke(this, new QueryFilterBypassEventArgs(bypassInfo));
    }

    public bool IsQueryBypass(string queryOrMethod)
    {
        if (string.IsNullOrWhiteSpace(queryOrMethod))
        {
            return false;
        }

        return BypassPatterns.IsMatch(queryOrMethod);
    }

    public void DetectAsNoTrackingWithExplicitFiltering(string? explicitFilter, string methodName)
    {
        if (string.IsNullOrWhiteSpace(explicitFilter) && methodName.Contains("AsNoTracking", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "SECURITY: AsNoTracking detected. Explicit tenant filtering must be verified. Method: {MethodName}",
                methodName);
        }
    }

    private void TriggerAlert(QueryFilterBypassInfo bypassInfo)
    {
        if (_logger.IsEnabled(LogLevel.Critical))
        {
            _logger.LogCritical(
                "ALERT: Query filter bypass attempt logged.{Newline}" +
                "Tenant={TenantId}, Method={MethodName}, Time={Time}",
                Environment.NewLine,
                bypassInfo.TenantId,
                bypassInfo.MethodName,
                bypassInfo.DetectedAt.ToString(CultureInfo.InvariantCulture));
        }
    }

    public static bool IsSafeQuery(string query, string currentTenantId)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        if (!query.Contains(currentTenantId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
}

public sealed class QueryFilterBypassInfo
{
    public required string MethodName { get; init; }
    public required DateTime DetectedAt { get; init; }
    public required string TenantId { get; init; }
    public required string StackTrace { get; init; }
    public string? ContextInfo { get; init; }
}

public sealed class QueryFilterBypassEventArgs : EventArgs
{
    public QueryFilterBypassInfo BypassInfo { get; }

    public QueryFilterBypassEventArgs(QueryFilterBypassInfo bypassInfo)
    {
        BypassInfo = bypassInfo ?? throw new ArgumentNullException(nameof(bypassInfo));
    }
}
