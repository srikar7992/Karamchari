namespace Karamchari.Core.Persistence.Filters;

/// <summary>
/// Decorator attribute used to mark entities or interfaces that must have
/// a global tenant query filter applied for security certification.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class TenantQueryFilterRequiredAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantQueryFilterRequiredAttribute"/> class.
    /// </summary>
    public TenantQueryFilterRequiredAttribute()
    {
    }
}
