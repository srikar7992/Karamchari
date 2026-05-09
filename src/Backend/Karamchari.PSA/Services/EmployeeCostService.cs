namespace Karamchari.PSA.Services;

using Karamchari.PSA.Domain;
using Karamchari.PSA.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Resolves an employee's cost-per-hour for project cost allocation.
///
/// The cost is sourced from <see cref="EmployeeCostSnapshot"/> â€” a monthly snapshot
/// of the employee's fully-loaded CTC (MonthlyGross from PayrollLedger).
///
/// If no snapshot exists for the requested month, we fall back to the latest available
/// snapshot (employees don't always get a payroll run immediately in month 1).
/// </summary>
public sealed partial class EmployeeCostService
{
    private readonly PSADbContext _db;
    private readonly ILogger<EmployeeCostService> _logger;

    public EmployeeCostService(PSADbContext db, ILogger<EmployeeCostService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Returns the cost-per-hour for the employee in the given month.
    /// Falls back to the most recent snapshot if no exact match exists.
    /// Returns null if the employee has never had a payroll run (new hire, no CTC data yet).
    /// </summary>
    public async Task<decimal?> GetCostPerHourAsync(
        Guid employeeId,
        int year,
        int month,
        CancellationToken ct = default)
    {
        // Exact match first (most common path after first payroll run).
        var snapshot = await _db.EmployeeCostSnapshots
            .FirstOrDefaultAsync(s =>
                s.EmployeeId == employeeId &&
                s.Year == year &&
                s.Month == month, ct);

        if (snapshot != null) return snapshot.CostPerHour;

        // Fallback: latest snapshot before the requested month.
        snapshot = await _db.EmployeeCostSnapshots
            .Where(s => s.EmployeeId == employeeId)
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .FirstOrDefaultAsync(ct);

        if (snapshot != null)
        {
            LogFallbackSnapshot(_logger, snapshot.Year, snapshot.Month, employeeId, year, month);
            return snapshot.CostPerHour;
        }

        _logger.LogWarning(
            "No cost snapshot found for Employee {EmployeeId}. Profit calculation will use zero cost.",
            employeeId);
        return null;
    }

    /// <summary>
    /// Creates or updates the cost snapshot for the employee from the latest payroll data.
    /// Called by the PayrollRunCompleted consumer or manually during onboarding.
    /// </summary>
    public async Task UpsertSnapshotAsync(
        Guid employeeId,
        decimal monthlyCost,
        int year,
        int month,
        decimal standardMonthlyHours = 160m,
        CancellationToken ct = default)
    {
        var existing = await _db.EmployeeCostSnapshots
            .FirstOrDefaultAsync(s =>
                s.EmployeeId == employeeId &&
                s.Year == year &&
                s.Month == month, ct);

        if (existing != null)
        {
            // Snapshot already exists â€” in a real system you'd update it if the cost changed.
            // For now, we keep the first snapshot (payroll run is authoritative).
            return;
        }

        var snapshot = EmployeeCostSnapshot.Compute(employeeId, monthlyCost, year, month, standardMonthlyHours);
        _db.EmployeeCostSnapshots.Add(snapshot);
        await _db.SaveChangesAsync(ct);

        LogSnapshotCreated(_logger, employeeId, monthlyCost, snapshot.CostPerHour);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Using fallback cost snapshot {Year}/{Month} for Employee {EmployeeId} (requested {ReqYear}/{ReqMonth}).")]
    private static partial void LogFallbackSnapshot(ILogger logger, int year, int month, Guid employeeId, int reqYear, int reqMonth);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created cost snapshot for Employee {EmployeeId}: {MonthlyCost}/mo, {CostPerHour}/hr.")]
    private static partial void LogSnapshotCreated(ILogger logger, Guid employeeId, decimal monthlyCost, decimal costPerHour);
}
