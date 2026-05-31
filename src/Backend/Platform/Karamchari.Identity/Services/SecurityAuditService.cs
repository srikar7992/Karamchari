namespace Karamchari.Identity.Services;

/// <summary>
/// Service for auditing security-sensitive events.
/// </summary>
public interface ISecurityAuditService
{
    /// <summary>Logs a successful authentication event.</summary>
    void LogAuthSuccess(string email, Guid userId, string tenantId, string? deviceInfo = null);

    /// <summary>Logs a failed authentication attempt.</summary>
    void LogAuthFailure(string email, string reason, string? tenantId = null, string? ip = null);

    /// <summary>Logs a token revocation event.</summary>
    void LogTokenRevocation(string jti, string reason, string userId);

    /// <summary>Logs suspicious activity detected by the security system.</summary>
    void LogSuspiciousActivity(string userId, string activity, string details);
}
