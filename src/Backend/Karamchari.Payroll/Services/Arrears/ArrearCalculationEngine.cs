using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.Arrears;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Payroll.Services.Arrears;

public record ArrearCalculationRequest(
    string TenantId,
    Guid EmployeeId,
    string EmployeeName,
    ArrearTriggerType TriggerType,
    string TriggerReference,
    DateOnly EffectiveFrom,
    DateOnly EffectiveTo,
    string InitiatedBy);

public record ArrearCalculationEngineResult(
    ArrearCalculation Calculation,
    IReadOnlyList<ArrearPeriodDiff> PeriodDiffs);

/// <summary>
/// Computes retroactive arrear differentials across impacted payroll periods.
/// Reads existing ledger entries as baseline; recomputes via CTCBreakdownService.
/// Replay-safe: always produces the same result for the same input.
/// </summary>
public sealed class ArrearCalculationEngine
{
    private readonly PayrollDbContext _db;

    public ArrearCalculationEngine(PayrollDbContext db)
    {
        _db = db;
    }

    public async Task<ArrearCalculationEngineResult> ComputeAsync(
        ArrearCalculationRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var calculation = ArrearCalculation.Create(
            request.TenantId,
            request.EmployeeId,
            request.EmployeeName,
            request.TriggerType,
            request.TriggerReference,
            request.EffectiveFrom,
            request.EffectiveTo,
            request.InitiatedBy);

        // Collect affected periods: every month between EffectiveFrom and EffectiveTo
        var periods = GetAffectedPeriods(request.EffectiveFrom, request.EffectiveTo);

        // Load existing ledger records for baseline
        var existingLedger = await _db.PayrollLedger
            .AsNoTracking()
            .Where(l => l.EmployeeId == request.EmployeeId
                && periods.Any(p => p.Year == l.Year && p.Month == l.Month))
            .ToListAsync(ct);

        // Load current (revised) payroll profile for recalculation
        var profile = await _db.PayrollProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.EmployeeId == request.EmployeeId, ct)
            ?? throw new InvalidOperationException($"No payroll profile for employee {request.EmployeeId}.");

        var diffs = new List<ArrearPeriodDiff>();

        foreach (var period in periods)
        {
            var existing = existingLedger.FirstOrDefault(l => l.Year == period.Year && l.Month == period.Month);

            if (existing is null)
                continue;  // period not yet processed — no arrear

            // Simplified recalculation: use current base salary as revised amount
            // Real implementation: plug into CTCBreakdownService + StatutoryPipelineEngine
            var revisedMonthlyGross = profile.BaseSalary;
            var revisedNet = revisedMonthlyGross * 0.85m;  // placeholder — statutory pipeline applies
            var revisedTds = revisedMonthlyGross * 0.10m;

            if (Math.Abs(revisedMonthlyGross - existing.MonthlyGross) < 1m)
                continue;  // no meaningful diff

            diffs.Add(new ArrearPeriodDiff(
                PeriodName: $"{period.Year}-{period.Month:D2}",
                Year: period.Year,
                Month: period.Month,
                OriginalGross: existing.MonthlyGross,
                RecalculatedGross: revisedMonthlyGross,
                OriginalNet: existing.NetPay,
                RecalculatedNet: revisedNet,
                OriginalTds: existing.TdsDeducted,
                RecalculatedTds: revisedTds,
                ComponentDeltas: new Dictionary<string, decimal>
                {
                    ["Basic"] = revisedMonthlyGross - existing.MonthlyGross
                }));
        }

        if (diffs.Count > 0)
            calculation.SetPeriodDiffs(diffs);

        return new ArrearCalculationEngineResult(calculation, diffs);
    }

    private static List<(int Year, int Month)> GetAffectedPeriods(DateOnly from, DateOnly to)
    {
        var periods = new List<(int Year, int Month)>();
        var current = new DateOnly(from.Year, from.Month, 1);
        var end = new DateOnly(to.Year, to.Month, 1);

        while (current <= end)
        {
            periods.Add((current.Year, current.Month));
            current = current.AddMonths(1);
        }

        return periods;
    }
}
