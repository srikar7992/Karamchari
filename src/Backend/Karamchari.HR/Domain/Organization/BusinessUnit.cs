using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.HR.Domain.Organization;

/// <summary>
/// Represents a high-level business unit or division.
/// </summary>
public sealed class BusinessUnit : AggregateRoot<Guid>, ITenantOwned
{
    private BusinessUnit() { }

    private BusinessUnit(Guid id, string tenantId, string name, string code) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Code = code;
        IsActive = true;
    }

    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public static BusinessUnit Create(string tenantId, string name, string code)
    {
        return new BusinessUnit(Guid.NewGuid(), tenantId, name.Trim(), code.Trim().ToUpperInvariant());
    }
}
