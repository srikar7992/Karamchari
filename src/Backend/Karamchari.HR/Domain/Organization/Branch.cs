using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.HR.Domain.Organization;

/// <summary>
/// Represents a physical office or branch location.
/// </summary>
public sealed class Branch : AggregateRoot<Guid>, ITenantOwned
{
    private Branch() { }

    private Branch(Guid id, string tenantId, string name, string code, string timeZoneId) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Code = code;
        TimeZoneId = timeZoneId;
        IsActive = true;
    }

    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string TimeZoneId { get; private set; } = "UTC";
    public bool IsActive { get; private set; }

    public static Branch Create(string tenantId, string name, string code, string timeZoneId = "UTC")
    {
        return new Branch(Guid.NewGuid(), tenantId, name.Trim(), code.Trim().ToUpperInvariant(), timeZoneId);
    }
}
