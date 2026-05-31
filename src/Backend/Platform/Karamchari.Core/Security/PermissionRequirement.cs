using Microsoft.AspNetCore.Authorization;

namespace Karamchari.Core.Security;

/// <summary>
/// Authorization requirement for a specific platform permission.
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        Permission = permission;
    }
}
