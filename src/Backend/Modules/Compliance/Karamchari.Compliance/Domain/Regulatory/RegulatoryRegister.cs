using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Compliance.Domain.Regulatory;

/// <summary>
/// Centralized registry of labor laws and regulations applicable to a tenant.
/// Example: India / Factories Act / Max Weekly Hours = 48.
/// </summary>
public sealed class RegulatoryRegister : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string RegulationCode { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime EffectiveDate { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private RegulatoryRegister() { }

    public static RegulatoryRegister Create(
        string tenantId,
        string country,
        string state,
        string regulationCode,
        string title,
        string description,
        DateTime effectiveDate,
        DateTime? expiryDate = null)
    {
        return new RegulatoryRegister
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Country = country,
            State = state,
            RegulationCode = regulationCode,
            Title = title,
            Description = description,
            EffectiveDate = effectiveDate,
            ExpiryDate = expiryDate,
            IsActive = true
        };
    }
}
