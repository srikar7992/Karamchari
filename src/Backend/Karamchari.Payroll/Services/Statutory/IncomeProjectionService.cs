namespace Karamchari.Payroll.Services.Statutory;

using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Projects annual income by combining YTD actuals with future estimates.
/// </summary>
public sealed class IncomeProjectionService : IIncomeProjectionService
{
    private readonly PayrollDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="IncomeProjectionService"/> class.
    /// </summary>
    public IncomeProjectionService(PayrollDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc/>
    public async Task<AnnualProjection> ProjectAnnualIncomeAsync(StatutoryContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 1. Fetch YTD Actuals from Ledger
        var ledgerEntries = await _dbContext.PayrollLedger
            .Where(e => e.EmployeeId == context.Profile.EmployeeId)
            .Where(e => e.FinancialYearStart == context.Year.StartYear)
            .ToListAsync();

        decimal ytdGross = ledgerEntries.Sum(e => e.MonthlyGross);
        decimal ytdTds = ledgerEntries.Sum(e => e.TdsDeducted);

        // 2. Determine Remaining Months
        // Indian FY starts in April (Month 4).
        // If current month is April (4), remaining = 12 (including current).
        // If current month is March (3), remaining = 1.

        int currentMonth = context.PayrollMonth;
        int monthsProcessed = ledgerEntries.Count; // This assumes entries exist for all past months

        // Alternatively, calculate based on month index
        // April=0, May=1, ..., March=11
        int fyMonthIndex = GetFyMonthIndex(currentMonth);
        int remainingMonths = 12 - fyMonthIndex;

        // 3. Project Future
        decimal projectedFutureGross = context.Breakdown.MonthlyGross * remainingMonths;

        return new AnnualProjection(ytdGross, ytdTds, projectedFutureGross, remainingMonths);
    }

    private static int GetFyMonthIndex(int month)
    {
        // 4 -> 0, 5 -> 1, ..., 12 -> 8, 1 -> 9, 2 -> 10, 3 -> 11
        return (month + 8) % 12;
    }
}
