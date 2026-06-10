// -----------------------------------------------------------------------
// <copyright file="BffClaimsExtensions.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;

namespace Karamchari.Api.BFF;

internal static class BffClaimsExtensions
{
    /// <summary>
    /// Extracts the employee ID from the JWT 'sub' claim.
    /// Returns null if absent or invalid â€” callers should return 401.
    /// </summary>
    internal static Guid? GetEmployeeId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var parsed) ? parsed : null;
    }

    /// <summary>
    /// Extracts the tenant ID from the JWT 'tenant_id' claim.
    /// Returns null if absent â€” callers must return 401.
    /// </summary>
    internal static string? GetTenantId(this ClaimsPrincipal user) =>
        user.FindFirstValue("tenant_id");

    internal static string? GetEmployeeIdString(this ClaimsPrincipal user) =>
        user.GetEmployeeId()?.ToString();

    /// <summary>
    /// Utility to get both TenantId (from claims) and EmployeeId (from 'sub' claim) in one call.
    /// Either value may be null â€” callers must return 401 when that happens.
    /// </summary>
    internal static (string? TenantId, Guid? EmployeeId) GetTenantAndEmployee(this ClaimsPrincipal user)
    {
        return (user.GetTenantId(), user.GetEmployeeId());
    }
}
