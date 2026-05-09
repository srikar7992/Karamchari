using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace Karamchari.Identity.Services;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public interface ISecurityAuditService
{
    /// <inheritdoc/>
    void LogAuthSuccess(string email, Guid userId, string tenantId, string? deviceInfo = null);
    /// <inheritdoc/>
    void LogAuthFailure(string email, string reason, string? tenantId = null, string? ip = null);
    /// <inheritdoc/>
    void LogTokenRevocation(string jti, string reason, string userId);
    /// <inheritdoc/>
    void LogSuspiciousActivity(string userId, string activity, string details);
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class SecurityAuditService : ISecurityAuditService
{
    private readonly ILogger<SecurityAuditService> _logger;

    /// <inheritdoc/>
    public SecurityAuditService(ILogger<SecurityAuditService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void LogAuthSuccess(string email, Guid userId, string tenantId, string? deviceInfo = null)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "AUTH_SUCCESS: User {Email} ({UserId}) logged in for tenant {TenantId}. Device: {DeviceInfo}",
                email, userId, tenantId, deviceInfo ?? "Unknown");
        }
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void LogAuthFailure(string email, string reason, string? tenantId = null, string? ip = null)
    {
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning(
                "AUTH_FAILURE: User {Email} failed login. Reason: {Reason}. Tenant: {TenantId}. IP: {IP}",
                email, reason, tenantId ?? "Unknown", ip ?? "Unknown");
        }
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void LogTokenRevocation(string jti, string reason, string userId)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "TOKEN_REVOKED: JTI {Jti} revoked for user {UserId}. Reason: {Reason}",
                jti, userId, reason);
        }
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void LogSuspiciousActivity(string userId, string activity, string details)
    {
        if (_logger.IsEnabled(LogLevel.Critical))
        {
            _logger.LogCritical(
                "SUSPICIOUS_ACTIVITY: User {UserId} performed {Activity}. Details: {Details}",
                userId, activity, details);
        }
    }
}
