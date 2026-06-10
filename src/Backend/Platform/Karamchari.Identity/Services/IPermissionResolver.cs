// -----------------------------------------------------------------------
// <copyright file="IPermissionResolver.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Identity.Services;

/// <summary>
/// Service responsible for resolving the set of permissions associated with a set of roles.
/// </summary>
public interface IPermissionResolver
{
    /// <summary>
    /// Gets the current version of the permission catalog.
    /// </summary>
    string GetPermissionVersion();

    /// <summary>
    /// Resolves the collection of permissions granted to the specified roles.
    /// </summary>
    /// <param name="roles">The roles to resolve permissions for.</param>
    /// <returns>A collection of unique permission strings.</returns>
    IReadOnlyCollection<string> ResolvePermissions(IEnumerable<string> roles);
}
