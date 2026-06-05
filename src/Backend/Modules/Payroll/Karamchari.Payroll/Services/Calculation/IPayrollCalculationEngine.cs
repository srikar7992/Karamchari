using Karamchari.Payroll.Domain.Results;

namespace Karamchari.Payroll.Services.Calculation;

/// <summary>
/// Input for calculating one employee's payroll from a snapshot.
/// All data sourced from the immutable PayrollCalculationSnapshot — never live records.
/// Includes version IDs for compensation and tax rules so a rerun in 2028 uses the
/// same rule versions as the original 2026 run, making results reproducible.
/// </summary>
public sealed record EmployeePayrollInput(
    Guid EmployeeId,
    string EmployeeName,
    decimal MonthlySalary,
    decimal HourlyRate,
    string Currency,
    decimal BasicPayPercent,          // % of gross that is basic (e.g., 40%)
    decimal DaPercent,                // % of basic that is DA
    IReadOnlyList<DayAttendanceRecord> AttendanceRecords,
    IReadOnlyList<HolidayRecord> Holidays,
    OvertimePolicySnapshot OvertimePolicy,
    IReadOnlyList<ShiftPremiumSnapshot> ShiftPremiums,
    IReadOnlyList<PendingAdjustmentRecord> Adjustments,
    bool OptedVoluntaryPf,
    bool IsEsicLocked,
    string StateCode,
    string TaxRegime,
    decimal YearToDateGross,
    decimal YearToDateTds,
    decimal DeclaredInvestments,
    int Month,
    int FinancialYear,
    // Versioning fields — ensure deterministic rerun
    Guid CompensationProfileId,        // which compensation profile version was active
    string TaxRuleVersionId,           // which TaxRuleVersion was used for TDS
    StatutoryConfigSnapshot StatutoryConfig);  // frozen statutory ceilings at time of run

public sealed record DayAttendanceRecord(
    DateOnly Date,
    decimal HoursWorked,
    bool IsPresent,
    bool IsLeave,
    bool IsHoliday,
    TimeOnly? ShiftStart,
    TimeOnly? ShiftEnd);

public sealed record HolidayRecord(DateOnly Date, string Name);

public sealed record OvertimePolicySnapshot(
    IReadOnlyList<OvertimeRuleSnapshot> Rules);

public sealed record OvertimeRuleSnapshot(
    decimal FromHours,
    decimal? ToHours,
    decimal Multiplier,
    string Context,
    int Priority);

public sealed record ShiftPremiumSnapshot(
    string PremiumType,
    decimal PremiumPercentage,
    TimeOnly? WindowStart,
    TimeOnly? WindowEnd,
    bool AppliesToWeekends,
    bool AppliesToHolidays);

public sealed record PendingAdjustmentRecord(
    Guid AdjustmentId,
    decimal Amount,
    string Type,   // Credit | Debit
    string Reason);

/// <summary>
/// Frozen statutory ceilings at snapshot time. Ensures PF/ESI calculations use
/// the same thresholds as the original run even if government revises them later.
/// </summary>
public sealed record StatutoryConfigSnapshot(
    decimal PfWageCeiling,             // default 15,000
    decimal EsiGrossCeiling,           // default 21,000
    decimal EsiEmployeeRate,           // default 0.0075
    decimal EsiEmployerRate,           // default 0.0325
    decimal PfEmployeeRate,            // default 0.12
    decimal PfEmployerEpfRate,         // default 0.0367
    decimal PfEmployerEpsMax);         // default 1,250

/// <summary>
/// Calculates EmployeePayrollResult from immutable snapshot inputs.
/// Deterministic: same inputs always produce same outputs.
/// </summary>
public interface IPayrollCalculationEngine
{
    EmployeePayrollResult Calculate(
        string tenantId,
        Guid payrollRunId,
        string periodName,
        int version,
        string snapshotId,
        EmployeePayrollInput input,
        ITaxCalculatorService taxCalculatorService);
}
