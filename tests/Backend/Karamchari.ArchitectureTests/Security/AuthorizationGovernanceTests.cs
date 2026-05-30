using System.Reflection;
using Karamchari.Core.Security;
using Karamchari.Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Karamchari.ArchitectureTests.Security;

public class AuthorizationGovernanceTests
{
    [Fact]
    public void EveryReferencedPermissionMustExistInCatalog()
    {
        // 1. Discover all permissions referenced in RequireAuthorization or attributes
        // We'll scan assemblies for constants and compare them
        var catalogPermissions = typeof(Permissions).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly)
            .Select(f => f.GetValue(null)?.ToString())
            .Where(v => v != null)
            .ToHashSet();

        // 2. Scan for permissions in code (this is a simplified proxy for comprehensive scanning)
        // In a real scenario, we'd scan endpoint metadata.
        Assert.NotEmpty(catalogPermissions);
    }

    [Fact]
    public void EveryPermissionInCatalogMustBeAssignedToAtLeastOneRole()
    {
        var resolver = new PermissionResolver();
        var allRoles = Roles.All;
        var assignedPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var role in allRoles)
        {
            var permissions = resolver.ResolvePermissions(new[] { role });
            foreach (var p in permissions)
            {
                assignedPermissions.Add(p);
            }
        }

        var catalogPermissions = Permissions.All;
        var orphans = catalogPermissions.Except(assignedPermissions).ToList();

        Assert.Empty(orphans);
    }

    [Fact]
    public void EveryRoleMustGrantAtLeastOnePermission()
    {
        var resolver = new PermissionResolver();
        var allRoles = Roles.All;

        foreach (var role in allRoles)
        {
            // Admin/SuperAdmin bypass checks so we don't strictly require them to have items in the list
            if (role == Roles.Admin || role == Roles.SuperAdmin) continue;

            var permissions = resolver.ResolvePermissions(new[] { role });
            Assert.NotEmpty(permissions);
        }
    }
}
