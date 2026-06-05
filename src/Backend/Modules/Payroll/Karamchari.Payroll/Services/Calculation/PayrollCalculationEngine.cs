using Karamchari.Payroll.Domain.DeductionRules;
using Karamchari.Payroll.Domain.Results;

namespace Karamchari.Payroll.Services.Calculation;

/// <summary>
/// Deterministic payroll calculation engine.
/// Pipeline: attendance → base pay → OT → holiday pay → premiums → deductions → tax → net.
/// All inputs come from an immutable snapshot. Never reads live attendance.
/// </summary>
public sealed class PayrollCalculationEngine : IPayrollCalculationEngine
{
    private readonly IEnumerable<IDeductionCalculator> _deductionCalculators;

    public PayrollCalculationEngine(IEnumerable<IDeductionCalculator> deductionCalculators)
    {
        _deductionCalculators = deductionCalculators;
    }

    public EmployeePayrollResult Calculate(
        string tenantId,
        Guid payrollRunId,
        string periodName,
        int version,
        string snapshotId,
        EmployeePayrollInput input,
        ITaxCalculatorService taxCalculatorService)
    {
        // ── Step 1: Attendance analysis ────────────────────────────────────
        var totalHoursWorked = input.AttendanceRecords
            .Where(r => r.IsPresent && !r.IsLeave)
            .Sum(r => r.HoursWorked);

        var holidayDaysWorked = input.AttendanceRecords
            .Where(r => r.IsPresent && r.IsHoliday)
            .ToList();

        var workingDaysInPeriod = input.AttendanceRecords.Count(r => !r.IsHoliday);
        var daysPresent = input.AttendanceRecords.Count(r => r.IsPresent && !r.IsLeave && !r.IsHoliday);

        // ── Step 2: Base pay (prorate if absence) ──────────────────────────
        var basePay = workingDaysInPeriod > 0
            ? Math.Round(input.MonthlySalary * daysPresent / workingDaysInPeriod, 2)
            : 0m;

        var basicPay = Math.Round(basePay * input.BasicPayPercent / 100m, 2);
        var da = Math.Round(basicPay * input.DaPercent / 100m, 2);

        // ── Step 3: Overtime pay ───────────────────────────────────────────
        var overtimePay = CalculateOvertimePay(input, totalHoursWorked);

        // ── Step 4: Holiday pay ────────────────────────────────────────────
        var dailyRate = input.MonthlySalary / workingDaysInPeriod;
        var holidayPay = holidayDaysWorked.Count * dailyRate;

        // ── Step 5: Shift premiums ─────────────────────────────────────────
        var shiftPremium = CalculateShiftPremium(input, basePay);

        // ── Step 6: Gross ──────────────────────────────────────────────────
        var grossPay = basePay + overtimePay + holidayPay + shiftPremium;

        // ── Step 7: Statutory deductions ──────────────────────────────────
        var deductionCtx = new DeductionContext(
            input.EmployeeId,
            tenantId,
            grossPay,
            basicPay,
            da,
            input.OptedVoluntaryPf,
            input.IsEsicLocked,
            input.StateCode,
            input.Month,
            input.FinancialYear);

        var pf = GetDeduction(StatutoryDeductionType.ProvidentFund, deductionCtx);
        var esi = GetDeduction(StatutoryDeductionType.EmployeeStateInsurance, deductionCtx);
        var pt = GetDeduction(StatutoryDeductionType.ProfessionalTax, deductionCtx);

        // ── Step 8: Tax ────────────────────────────────────────────────────
        var taxInput = new TaxCalculationInput(
            input.EmployeeId,
            input.TaxRegime,
            input.Month,
            input.FinancialYear,
            input.YearToDateGross,
            input.YearToDateTds,
            grossPay,
            pf,
            pt,
            input.DeclaredInvestments);

        var taxOutput = taxCalculatorService.Calculate(taxInput);
        var tds = taxOutput.TdsThisMonth;

        // ── Step 9: Adjustments ────────────────────────────────────────────
        var credits = input.Adjustments.Where(a => a.Type == "Credit").Sum(a => a.Amount);
        var debits = input.Adjustments.Where(a => a.Type == "Debit").Sum(a => a.Amount);
        var netAdjustments = credits - debits;

        // ── Step 10: Assemble result ───────────────────────────────────────
        var earnings = new EarningsBreakdown(basePay, overtimePay, holidayPay, shiftPremium, 0m, 0m);
        var deductions = new DeductionsBreakdown(pf, esi, pt, tds, 0m);

        return EmployeePayrollResult.Calculate(
            tenantId,
            payrollRunId,
            input.EmployeeId,
            input.EmployeeName,
            periodName,
            version,
            snapshotId,
            earnings,
            deductions,
            retroAdjustments: 0m,
            manualAdjustments: netAdjustments);
    }

    private static decimal CalculateOvertimePay(EmployeePayrollInput input, decimal totalHours)
    {
        if (!input.OvertimePolicy.Rules.Any()) return 0m;

        var regularThreshold = 8m * input.AttendanceRecords.Count(r => r.IsPresent);
        var overtimeHours = Math.Max(0m, totalHours - regularThreshold);
        if (overtimeHours == 0m) return 0m;

        var hourlyRate = input.HourlyRate > 0
            ? input.HourlyRate
            : input.MonthlySalary / (input.AttendanceRecords.Count * 8m);

        var sortedRules = input.OvertimePolicy.Rules
            .OrderBy(r => r.Priority)
            .ToList();

        var remaining = overtimeHours;
        var totalPay = 0m;

        foreach (var rule in sortedRules)
        {
            if (remaining <= 0m) break;

            var capacity = rule.ToHours.HasValue
                ? Math.Min(rule.ToHours.Value - rule.FromHours, remaining)
                : remaining;

            var hours = Math.Min(capacity, remaining);
            totalPay += hourlyRate * rule.Multiplier * hours;
            remaining -= hours;
        }

        return Math.Round(totalPay, 2);
    }

    private static decimal CalculateShiftPremium(EmployeePayrollInput input, decimal basePay)
    {
        var totalPremium = 0m;

        foreach (var record in input.AttendanceRecords.Where(r => r.IsPresent))
        {
            var isHoliday = input.Holidays.Any(h => h.Date == record.Date);

            foreach (var premiumRule in input.ShiftPremiums)
            {
                bool applies = false;

                if (premiumRule.WindowStart.HasValue && premiumRule.WindowEnd.HasValue && record.ShiftStart.HasValue)
                {
                    var start = premiumRule.WindowStart.Value;
                    var end = premiumRule.WindowEnd.Value;
                    var shiftTime = record.ShiftStart.Value;

                    applies = start <= end
                        ? shiftTime >= start && shiftTime <= end
                        : shiftTime >= start || shiftTime <= end;
                }
                else if (premiumRule.AppliesToHolidays && isHoliday)
                {
                    applies = true;
                }
                else if (premiumRule.AppliesToWeekends &&
                         (record.Date.DayOfWeek == DayOfWeek.Saturday || record.Date.DayOfWeek == DayOfWeek.Sunday))
                {
                    applies = true;
                }

                if (applies)
                {
                    var dailyBase = basePay / input.AttendanceRecords.Count(r => r.IsPresent);
                    totalPremium += dailyBase * (premiumRule.PremiumPercentage / 100m);
                }
            }
        }

        return Math.Round(totalPremium, 2);
    }

    private decimal GetDeduction(StatutoryDeductionType type, DeductionContext ctx)
    {
        var calculator = _deductionCalculators.FirstOrDefault(c => c.DeductionType == type);
        return calculator?.Calculate(ctx) ?? 0m;
    }
}
