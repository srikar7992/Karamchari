namespace Karamchari.Identity.Domain;

/// <summary>
/// Represents a security-sensitive event for auditing purposes.
/// Tracks authentication attempts, token lifecycle events, and suspicious activities.
/// </summary>
public sealed class SecurityAuditEntry
{
    private SecurityAuditEntry()
    {
        // EF Core
        EventName = string.Empty;
        Details = string.Empty;
    }

    private SecurityAuditEntry(
        Guid id,
        string eventName,
        string? userId,
        string? tenantId,
        string? ipAddress,
        string details,
        bool isSuccess)
    {
        Id = id;
        EventName = eventName;
        UserId = userId;
        TenantId = tenantId;
        IpAddress = ipAddress;
        Details = details;
        IsSuccess = isSuccess;
        TimestampUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the name of the security event.</summary>
    public string EventName { get; private set; }

    /// <summary>Gets the user identifier, if applicable.</summary>
    public string? UserId { get; private set; }

    /// <summary>Gets the tenant identifier, if applicable.</summary>
    public string? TenantId { get; private set; }

    /// <summary>Gets the source IP address.</summary>
    public string? IpAddress { get; private set; }

    /// <summary>Gets the event details.</summary>
    public string Details { get; private set; }

    /// <summary>Gets a value indicating whether the event was a success.</summary>
    public bool IsSuccess { get; private set; }

    /// <summary>Gets the timestamp of the event.</summary>
    public DateTimeOffset TimestampUtc { get; private set; }

    /// <summary>Creates a new security audit entry.</summary>
    public static SecurityAuditEntry Create(
        string eventName,
        string? userId,
        string? tenantId,
        string? ipAddress,
        string details,
        bool isSuccess = true)
    {
        return new SecurityAuditEntry(
            Guid.NewGuid(),
            eventName,
            userId,
            tenantId,
            ipAddress,
            details,
            isSuccess);
    }
}
