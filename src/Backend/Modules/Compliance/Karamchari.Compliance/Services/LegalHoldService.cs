using Karamchari.Compliance.Domain.LegalHold;
using Karamchari.Compliance.Domain.Retention;
using Karamchari.Compliance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Compliance.Services;

/// <summary>
/// Enforces legal hold rules before any retention action executes.
/// A record is protected if any active hold's scope covers it.
/// </summary>
public sealed class LegalHoldService
{
    private readonly ComplianceDbContext _db;

    public LegalHoldService(ComplianceDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns true if the given record is covered by at least one active legal hold.
    /// Checks Employee scope (scopeValue = employeeId.ToString()),
    /// AllTenant scope, and AttendancePeriod / PayrollPeriod / Site scopes by recordType.
    /// </summary>
    public async Task<bool> IsUnderHoldAsync(
        string tenantId,
        RetentionRecordType recordType,
        string recordId,
        CancellationToken ct = default)
    {
        var activeHolds = await _db.LegalHolds
            .Include(h => h.Scopes)
            .Where(h => h.TenantId == tenantId && h.Status == LegalHoldStatus.Active)
            .ToListAsync(ct);

        foreach (var hold in activeHolds)
        {
            foreach (var scope in hold.Scopes)
            {
                if (scope.ScopeType == LegalHoldScopeType.AllTenant)
                    return true;

                // Employee scope: scopeValue is the employeeId string
                if (scope.ScopeType == LegalHoldScopeType.Employee && scope.ScopeValue == recordId)
                    return true;

                // Period scopes: scopeValue is "YYYY-MM" or a period key
                if (scope.ScopeType == LegalHoldScopeType.PayrollPeriod && recordType == RetentionRecordType.Payroll)
                    return true;

                if (scope.ScopeType == LegalHoldScopeType.AttendancePeriod && recordType == RetentionRecordType.Attendance)
                    return true;
            }
        }

        return false;
    }
}
