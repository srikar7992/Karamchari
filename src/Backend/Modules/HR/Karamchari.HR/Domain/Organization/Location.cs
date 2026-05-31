using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.HR.Domain.Organization;

/// <summary>
/// Represents a physical work location (e.g. "Main Office", "Warehouse A").
/// </summary>
public sealed class Location : AggregateRoot<Guid>, ITenantOwned
{
    private Location() { }

    private Location(Guid id, string tenantId, string name, string address) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Address = address;
        IsActive = true;
    }

    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public static Location Create(string tenantId, string name, string address)
    {
        return new Location(Guid.NewGuid(), tenantId, name.Trim(), address.Trim());
    }
}
