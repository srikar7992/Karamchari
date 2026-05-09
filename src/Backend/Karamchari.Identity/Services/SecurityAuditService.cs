using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace Karamchari.Identity.Services;

public interface ISecurityAuditService
{
    void LogAuthSuccess(string email, Guid userId, string tenantId, string? deviceInfo = null);
    void LogAuthFailure(string email, string reason, string? tenantId = null, string? ip = null);
    void LogTokenRevocation(string jti, string reason, string userId);
    void LogSuspiciousActivity(string userId, string activity, string details);
}

public sealed class SecurityAuditService : ISecurityAuditService
{
    private readonly ILogger<SecurityAuditService> _logger;

    public SecurityAuditService(ILogger<SecurityAuditService> logger)
    {
        _logger = logger;
    }

    public void LogAuthSuccess(string email, Guid userId, string tenantId, string? deviceInfo = null)
    {
        _logger.LogInformation(
            "AUTH_SUCCESS: User {Email} ({UserId}) logged in for tenant {TenantId}. Device: {DeviceInfo}",
            email, userId, tenantId, deviceInfo ?? "Unknown");
    }

    public void LogAuthFailure(string email, string reason, string? tenantId = null, string? ip = null)
    {
        _logger.LogWarning(
            "AUTH_FAILURE: User {Email} failed login. Reason: {Reason}. Tenant: {TenantId}. IP: {IP}",
            email, reason, tenantId ?? "Unknown", ip ?? "Unknown");
    }

    public void LogTokenRevocation(string jti, string reason, string userId)
    {
        _logger.LogInformation(
            "TOKEN_REVOKED: JTI {Jti} revoked for user {UserId}. Reason: {Reason}",
            jti, userId, reason);
    }

    public void LogSuspiciousActivity(string userId, string activity, string details)
    {
        _logger.LogCritical(
            "SUSPICIOUS_ACTIVITY: User {UserId} performed {Activity}. Details: {Details}",
            userId, activity, details);
    }
}
