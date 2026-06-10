// -----------------------------------------------------------------------
// <copyright file="AttendancePolicyHierarchyResolver.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.TimeAttendance.Domain.Policy;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Karamchari.TimeAttendance.Services;

/// <summary>
/// Resolves the effective attendance policy for an employee using the hierarchy:
/// Organisation → BusinessUnit → Department → ShiftType → Individual (most specific wins).
/// Results are cached per tenant for 5 minutes to avoid per-request DB round-trips.
/// </summary>
public sealed class AttendancePolicyHierarchyResolver(TimeAttendanceDbContext db, IMemoryCache cache)
{
    private readonly TimeAttendanceDbContext _db = db;
    private readonly IMemoryCache _cache = cache;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Resolves the effective policy for an employee.
    /// The most specific scope (Individual > ShiftType > Department > BusinessUnit > Organisation) wins.
    /// Individual scope requires explicit employee override; ShiftType scope requires the employee's active shift.
    /// </summary>
    public async Task<AttendancePolicyHierarchy> ResolveForEmployeeAsync(
        string tenantId,
        Guid employeeId,
        Guid? departmentId = null,
        Guid? businessUnitId = null,
        Guid? shiftTypeId = null,
        CancellationToken ct = default)
    {
        // Individual override — most specific
        AttendancePolicyHierarchy? individual = await GetCachedOrQueryAsync(
            $"policy:{tenantId}:individual:{employeeId}",
            () => _db.AttendancePolicyHierarchies
                .Where(p => p.TenantId == tenantId
                         && p.Scope == PolicyScope.Individual
                         && p.ScopeEntityId == employeeId)
                .FirstOrDefaultAsync(ct));

        if (individual is not null) return individual;

        // ShiftType override
        if (shiftTypeId.HasValue)
        {
            AttendancePolicyHierarchy? shiftPolicy = await GetCachedOrQueryAsync(
                $"policy:{tenantId}:shift:{shiftTypeId}",
                () => _db.AttendancePolicyHierarchies
                    .Where(p => p.TenantId == tenantId
                             && p.Scope == PolicyScope.ShiftType
                             && p.ScopeEntityId == shiftTypeId)
                    .FirstOrDefaultAsync(ct));

            if (shiftPolicy is not null) return shiftPolicy;
        }

        // Department override
        if (departmentId.HasValue)
        {
            AttendancePolicyHierarchy? deptPolicy = await GetCachedOrQueryAsync(
                $"policy:{tenantId}:dept:{departmentId}",
                () => _db.AttendancePolicyHierarchies
                    .Where(p => p.TenantId == tenantId
                             && p.Scope == PolicyScope.Department
                             && p.ScopeEntityId == departmentId)
                    .FirstOrDefaultAsync(ct));

            if (deptPolicy is not null) return deptPolicy;
        }

        // BusinessUnit override
        if (businessUnitId.HasValue)
        {
            AttendancePolicyHierarchy? buPolicy = await GetCachedOrQueryAsync(
                $"policy:{tenantId}:bu:{businessUnitId}",
                () => _db.AttendancePolicyHierarchies
                    .Where(p => p.TenantId == tenantId
                             && p.Scope == PolicyScope.BusinessUnit
                             && p.ScopeEntityId == businessUnitId)
                    .FirstOrDefaultAsync(ct));

            if (buPolicy is not null) return buPolicy;
        }

        // Organisation default — always exists (created during tenant provisioning)
        AttendancePolicyHierarchy? orgPolicy = await GetCachedOrQueryAsync(
            $"policy:{tenantId}:org",
            () => _db.AttendancePolicyHierarchies
                .Where(p => p.TenantId == tenantId && p.Scope == PolicyScope.Organisation)
                .FirstOrDefaultAsync(ct));

        return orgPolicy ?? AttendancePolicyHierarchy.CreateOrgDefault(tenantId, "System Default");
    }

    private async Task<AttendancePolicyHierarchy?> GetCachedOrQueryAsync(
        string key,
        Func<Task<AttendancePolicyHierarchy?>> query)
    {
        if (_cache.TryGetValue(key, out AttendancePolicyHierarchy? cached))
            return cached;

        AttendancePolicyHierarchy? result = await query();

        if (result is not null)
            _cache.Set(key, result, CacheTtl);

        return result;
    }
}
