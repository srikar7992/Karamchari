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
    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Authority { get; private set; } = string.Empty; // e.g. AWS, Microsoft
    public string LicenseNumber { get; private set; } = string.Empty;
    
    public DateTimeOffset IssuedAtUtc { get; private set; }
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    public CertificationStatus Status { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    private CertificationAchievement() { }

    public static CertificationAchievement Record(
        string tenantId,
        Guid employeeId,
        string name,
        string authority,
        string licenseNumber,
        DateTimeOffset issuedAt,
        DateTimeOffset? expiresAt)
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
            Status = CertificationStatus.Active
        };

        cert.EvaluateStatus();
        return cert;
    }

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

    public void Revoke(string reason)
    {
        Status = CertificationStatus.Revoked;
        // Raise CertificationRevokedEvent
    }
}
