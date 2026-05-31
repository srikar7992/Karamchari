using System.Globalization;
using System.Text.RegularExpressions;
using Karamchari.Core.Multitenancy;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Persistence.Filters;

/// <summary>
/// Detects attempts to bypass global tenant query filters in EF Core,
/// such as usage of <c>IgnoreQueryFilters()</c> or <c>AsNoTracking()</c>
/// without explicit tenant validation.
/// </summary>
public sealed class QueryFilterBypassDetector
{
    private static readonly Regex BypassPatterns = new(
        @"(?i)\b(ignorequeryfilters|ignoreallqueryfilters|asnotracking|no\btracking)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<QueryFilterBypassDetector> _logger;

    /// <summary>
    /// Event raised when a query filter bypass attempt is detected.
    /// </summary>
    public event EventHandler<QueryFilterBypassEventArgs>? BypassDetected;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryFilterBypassDetector"/> class.
    /// </summary>
    /// <param name="tenantProvider">The tenant provider to resolve current tenant context.</param>
    /// <param name="logger">The logger for security alerts.</param>
    public QueryFilterBypassDetector(
        ITenantProvider tenantProvider,
        ILogger<QueryFilterBypassDetector> logger)
    {
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Detects and logs a potential query filter bypass attempt.
    /// </summary>
    /// <param name="methodName">The name of the method where the bypass was detected.</param>
    /// <param name="contextInfo">Additional context information about the query.</param>
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

        TriggerAlert(bypassInfo);

        BypassDetected?.Invoke(this, new QueryFilterBypassEventArgs(bypassInfo));
    }

    /// <summary>
    /// Checks if a query string or method call contains bypass patterns.
    /// </summary>
    /// <param name="queryOrMethod">The string to check.</param>
    /// <returns>True if a bypass pattern is detected.</returns>
    public bool IsQueryBypass(string queryOrMethod)
    {
        if (string.IsNullOrWhiteSpace(queryOrMethod))
        {
            return false;
        }

        return BypassPatterns.IsMatch(queryOrMethod);
    }

    /// <summary>
    /// Detects usage of AsNoTracking and checks if explicit filtering is provided.
    /// </summary>
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
        _logger.LogCritical(
            "ALERT: Query filter bypass attempt logged.{Newline}" +
            "Tenant={TenantId}, Method={MethodName}, Time={Time}",
            Environment.NewLine,
            bypassInfo.TenantId,
            bypassInfo.MethodName,
            bypassInfo.DetectedAt.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Static utility to check if a raw SQL query contains the current tenant ID.
    /// </summary>
    public static bool IsSafeQuery(string query, string currentTenantId)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        return query.Contains(currentTenantId, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Information about a detected query filter bypass.
/// </summary>
public sealed class QueryFilterBypassInfo
{
    /// <summary>The name of the method that attempted the bypass.</summary>
    public required string MethodName { get; init; }

    /// <summary>The timestamp when the bypass was detected.</summary>
    public required DateTime DetectedAt { get; init; }

    /// <summary>The tenant ID associated with the execution context.</summary>
    public required string TenantId { get; init; }

    /// <summary>The stack trace of the bypass attempt.</summary>
    public required string StackTrace { get; init; }

    /// <summary>Optional diagnostic context info.</summary>
    public string? ContextInfo { get; init; }
}

/// <summary>
/// Event arguments for query filter bypass events.
/// </summary>
public sealed class QueryFilterBypassEventArgs : EventArgs
{
    /// <summary>Gets the detailed bypass information.</summary>
    public QueryFilterBypassInfo BypassInfo { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryFilterBypassEventArgs"/> class.
    /// </summary>
    public QueryFilterBypassEventArgs(QueryFilterBypassInfo bypassInfo)
    {
        BypassInfo = bypassInfo ?? throw new ArgumentNullException(nameof(bypassInfo));
    }
}
