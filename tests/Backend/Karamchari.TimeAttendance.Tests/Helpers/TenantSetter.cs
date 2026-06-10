// -----------------------------------------------------------------------
// <copyright file="TenantSetter.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Tests.Helpers;

using System.Reflection;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Test-only helper that sets TenantId on domain aggregates via reflection.
/// Replaces the EF interceptor that does this in production (which requires a real service provider).
/// Never use outside of test projects.
/// </summary>
internal static class TenantSetter
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>Sets TenantId on any ITenantOwned object. Returns the same object for fluent chaining.</summary>
    public static T WithTenant<T>(this T entity, string tenantId) where T : ITenantOwned
    {
        var prop = typeof(T).GetProperty("TenantId", Flags)
            ?? throw new InvalidOperationException($"{typeof(T).Name} has no TenantId property.");

        // Try backing field first (private set backing field), then setter
        var backingField = typeof(T).GetField("<TenantId>k__BackingField", Flags);
        if (backingField is not null)
            backingField.SetValue(entity, tenantId);
        else
            prop.SetValue(entity, tenantId);

        return entity;
    }

    /// <summary>Sets TenantId on a collection of ITenantOwned objects.</summary>
    public static IEnumerable<T> WithTenant<T>(this IEnumerable<T> entities, string tenantId) where T : ITenantOwned
        => entities.Select(e => e.WithTenant(tenantId));
}
