using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.HR.Domain.Organization;

/// <summary>
/// Represents a job title/rank within a tenant organization.
/// </summary>
public sealed class Designation : AggregateRoot<Guid>, ITenantOwned
{
    private Designation() { }

    private Designation(Guid id, string tenantId, string name, string code, int level) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Code = code;
        Level = level;
        IsActive = true;
    }

    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;

    /// <summary>Hierarchical level (e.g. 1 for entry, 10 for executive).</summary>
    public int Level { get; private set; }

    public bool IsActive { get; private set; }

    public static Designation Create(string tenantId, string name, string code, int level)
    {
        return new Designation(Guid.NewGuid(), tenantId, name.Trim(), code.Trim().ToUpperInvariant(), level);
    }
}
