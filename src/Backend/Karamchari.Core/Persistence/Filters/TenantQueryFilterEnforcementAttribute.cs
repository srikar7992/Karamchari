namespace Karamchari.Core.Persistence.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class TenantQueryFilterRequiredAttribute : Attribute
{
    public TenantQueryFilterRequiredAttribute()
    {
    }
}
