// -----------------------------------------------------------------------
// <copyright file="CompOffAccrualService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Domain.Policy;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.TimeAttendance.Services;

/// <summary>
/// Accrues comp-off credits when an employee works on a holiday or weekend beyond the policy threshold.
/// Also handles expiry: grants past expiry are either forfeited or converted to encashment per policy.
/// </summary>
public sealed class CompOffAccrualService(
    TimeAttendanceDbContext db,
    AttendancePolicyHierarchyResolver policyResolver,
    ILogger<CompOffAccrualService> logger)
{
    private readonly TimeAttendanceDbContext _db = db;
    private readonly AttendancePolicyHierarchyResolver _policyResolver = policyResolver;
    private readonly ILogger<CompOffAccrualService> _logger = logger;

    /// <summary>
    /// Called by the attendance finalization job when a holiday/weekend shift is completed.
    /// Accrues a comp-off grant if worked minutes exceed the policy trigger.
    /// </summary>
    public async Task AccrueIfEligibleAsync(
        string tenantId,
        Guid employeeId,
        DateOnly workDate,
        int workedMinutes,
        bool isHoliday,
        bool isWeekend,
        CancellationToken ct = default)
    {
        if (!isHoliday && !isWeekend) return;

        AttendancePolicyHierarchy policy = await _policyResolver.ResolveForEmployeeAsync(tenantId, employeeId, ct: ct);

        if (!policy.ShouldAccrueCompOff(workedMinutes)) return;

        // One comp-off grant per work date — idempotent
        bool existing = await _db.CompOffGrants
            .AnyAsync(g => g.TenantId == tenantId
                        && g.EmployeeId == employeeId
                        && g.WorkedOnDate == workDate, ct);

        if (existing)
        {
            _logger.LogDebug("CompOff already accrued for employee {EmployeeId} on {WorkDate}.", employeeId, workDate);
            return;
        }

        DateOnly expiryDate = workDate.AddDays(policy.CompOffExpiryDays);

        var grant = CompOffGrant.Create(
            tenantId,
            employeeId,
            workDate,
            daysEarned: 1.0m, // 1 comp-off day per qualifying session
            expiryDate: expiryDate);

        _db.CompOffGrants.Add(grant);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "CompOff accrued. Employee={EmployeeId} WorkDate={WorkDate} Expiry={Expiry}",
            employeeId, workDate, expiryDate);
    }

    /// <summary>
    /// Processes expired comp-off grants. Run as a nightly scheduled job.
    /// Grants past expiry are either forfeited or queued for encashment per policy.
    /// </summary>
    public async Task ProcessExpiredGrantsAsync(string tenantId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        List<CompOffGrant> expiredGrants = await _db.CompOffGrants
            .Where(g => g.TenantId == tenantId
                     && g.Status == CompOffGrantStatus.Active
                     && g.ExpiryDate < today)
            .ToListAsync(ct);

        if (expiredGrants.Count == 0) return;

        AttendancePolicyHierarchy policy = await _policyResolver.ResolveForEmployeeAsync(tenantId, Guid.Empty, ct: ct);

        foreach (CompOffGrant? grant in expiredGrants)
        {
            if (policy.CompOffPayableOnExpiry)
            {
                grant.MarkPayable();
                _logger.LogInformation(
                    "CompOff grant {GrantId} expired and queued for encashment.", grant.Id);
            }
            else
            {
                grant.Forfeit();
                _logger.LogInformation(
                    "CompOff grant {GrantId} expired and forfeited.", grant.Id);
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}
