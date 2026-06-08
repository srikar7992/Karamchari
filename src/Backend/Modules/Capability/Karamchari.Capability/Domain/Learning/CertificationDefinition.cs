using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Capability.Domain.Learning;

/// <summary>
/// Governed certification catalog entry. Employees earn CertificationAchievements
/// that reference this definition.
/// </summary>
public sealed class CertificationDefinition : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Issuer { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    /// <summary>How many months the certification is valid. Null = does not expire.</summary>
    public int? DefaultValidityMonths { get; private set; }

    /// <summary>How many months before expiry the renewal process should start.</summary>
    public int? RenewalLeadTimeMonths { get; private set; }

    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private CertificationDefinition() { }

    public static CertificationDefinition Create(
        string tenantId,
        string name,
        string issuer,
        string description,
        int? defaultValidityMonths = null,
        int? renewalLeadTimeMonths = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);

        if (defaultValidityMonths.HasValue && defaultValidityMonths.Value <= 0)
            throw new ArgumentException("Validity months must be positive.");
        if (renewalLeadTimeMonths.HasValue && renewalLeadTimeMonths.Value <= 0)
            throw new ArgumentException("Renewal lead time must be positive.");

        return new CertificationDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            Issuer = issuer.Trim(),
            Description = description,
            DefaultValidityMonths = defaultValidityMonths,
            RenewalLeadTimeMonths = renewalLeadTimeMonths,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void Update(string name, string issuer, string description, int? defaultValidityMonths, int? renewalLeadTimeMonths)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);

        Name = name.Trim();
        Issuer = issuer.Trim();
        Description = description;
        DefaultValidityMonths = defaultValidityMonths;
        RenewalLeadTimeMonths = renewalLeadTimeMonths;
    }

    public void Deactivate() => IsActive = false;
}
