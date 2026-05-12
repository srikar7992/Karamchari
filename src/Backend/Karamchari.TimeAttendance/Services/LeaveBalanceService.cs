using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.TimeAttendance.Services;

/// <summary>
/// Domain service for calculating leave balances from the immutable ledger.
/// Implements Priority 1 Step 4.
/// </summary>
public sealed class LeaveBalanceService
{
    private readonly TimeAttendanceDbContext _db;

    public LeaveBalanceService(TimeAttendanceDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Calculates the current available balance for an employee from their ledger entries.
    /// </summary>
    public async Task<decimal> GetCurrentBalanceAsync(Guid employeeId, Guid policyId, CancellationToken ct = default)
    {
        var balance = await _db.LeaveBalances
            .Include(b => b.Entries)
            .FirstOrDefaultAsync(b => b.EmployeeId == employeeId && b.PolicyId == policyId, ct);

        return balance?.AvailableBalance ?? 0m;
    }

    /// <summary>
    /// Reconstructs the leave balance at a specific point in time.
    /// Implements Priority 1 Step 5 (Auditability).
    /// </summary>
    public async Task<decimal> GetHistoricalBalanceAsync(Guid employeeId, Guid policyId, DateTimeOffset asOf, CancellationToken ct = default)
    {
        var entries = await _db.Set<LeaveBalanceEntry>()
            .Where(e => e.EmployeeId == employeeId && e.PolicyId == policyId && e.CreatedAt <= asOf)
            .ToListAsync(ct);

        return entries.Sum(e => e.Quantity);
    }
}
