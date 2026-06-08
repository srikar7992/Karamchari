using Karamchari.Capability.Domain.Primitives;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Capability.Domain.Learning;

/// <summary>
/// Aggregate root representing an earned certification.
/// Handles expiration tracking and renewal lifecycles.
/// </summary>
public sealed class CertificationAchievement : AggregateRoot<Guid>, ITenantOwned
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EmployeeId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Authority { get; private set; } = string.Empty; // e.g. AWS, Microsoft
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string LicenseNumber { get; private set; } = string.Empty;

    /// <summary>Optional FK to governed CertificationDefinition catalog.</summary>
    public Guid? CertificationDefinitionId { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset IssuedAtUtc { get; private set; }
    /// <inheritdoc/>
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public CertificationStatus Status { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    private CertificationAchievement() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static CertificationAchievement Record(
        string tenantId,
        Guid employeeId,
        string name,
        string authority,
        string licenseNumber,
        DateTimeOffset issuedAt,
        DateTimeOffset? expiresAt,
        Guid? certificationDefinitionId = null)
    {
        var cert = new CertificationAchievement
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            Name = name,
            Authority = authority,
            LicenseNumber = licenseNumber,
            IssuedAtUtc = issuedAt,
            ExpiresAtUtc = expiresAt,
            Status = CertificationStatus.Active,
            CertificationDefinitionId = certificationDefinitionId
        };

        cert.EvaluateStatus();
        return cert;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void EvaluateStatus()
    {
        if (Status == CertificationStatus.Revoked) return;

        if (!ExpiresAtUtc.HasValue)
        {
            Status = CertificationStatus.Active;
            return;
        }

        var now = DateTimeOffset.UtcNow;
        if (now >= ExpiresAtUtc.Value)
        {
            Status = CertificationStatus.Expired;
            // Raise CertificationExpiredEvent
        }
        else if (now >= ExpiresAtUtc.Value.AddDays(-30)) // Warning window
        {
            Status = CertificationStatus.ExpiringSoon;
        }
        else
        {
            Status = CertificationStatus.Active;
        }
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Revoke(string reason)
    {
        Status = CertificationStatus.Revoked;
        // Raise CertificationRevokedEvent
    }
}
