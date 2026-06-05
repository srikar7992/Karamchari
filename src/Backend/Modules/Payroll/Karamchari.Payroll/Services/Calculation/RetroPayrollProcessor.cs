using System.Text.Json;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.Adjustments;
using Karamchari.Payroll.Domain.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Payroll.Services.Calculation;

/// <summary>
/// Processes attendance corrections against closed (Locked) payroll runs.
/// Never modifies the source payroll. Produces a RetroPayrollAdjustment
/// that carries the monetary difference into the next open run.
///
/// Algorithm:
///   1. Load the locked EmployeePayrollResult for the source run.
///   2. Load the immutable PayrollCalculationSnapshot for that run.
///   3. Deserialize the snapshot to find the employee's original input.
///   4. Apply the attendance correction to the input (replace hours on corrected date).
///   5. Recalculate using the same engine and tax service.
///   6. Diff: shouldHavePaid = recalculated.NetPay, actuallyPaid = lockedResult.NetPay.
///   7. If diff != 0, create and return RetroPayrollAdjustment.
/// </summary>
public sealed class RetroPayrollProcessor : IRetroPayrollProcessor
{
    private readonly PayrollDbContext _db;
    private readonly IPayrollCalculationEngine _engine;
    private readonly ITaxCalculatorService _taxService;
    private readonly ILogger<RetroPayrollProcessor> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public RetroPayrollProcessor(
        PayrollDbContext db,
        IPayrollCalculationEngine engine,
        ITaxCalculatorService taxService,
        ILogger<RetroPayrollProcessor> logger)
    {
        _db = db;
        _engine = engine;
        _taxService = taxService;
        _logger = logger;
    }

    public async Task<RetroPayrollAdjustment?> ProcessCorrectionAsync(
        AttendanceCorrectionEvent correction,
        CancellationToken cancellationToken = default)
    {
        // 1. Load locked result — must be Locked, not just Approved
        var lockedResult = await _db.EmployeePayrollResults
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.PayrollRunId == correction.SourcePayrollRunId &&
                r.EmployeeId == correction.EmployeeId &&
                r.Status == EmployeePayrollResultStatus.Locked,
                cancellationToken);

        if (lockedResult is null)
        {
            _logger.LogWarning(
                "Retro skipped: no locked result for Employee={EmployeeId}, Run={RunId}",
                correction.EmployeeId, correction.SourcePayrollRunId);
            return null;
        }

        // 2. Load snapshot
        var snapshot = await _db.PayrollCalculationSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.PayrollRunId == correction.SourcePayrollRunId,
                cancellationToken);

        if (snapshot is null)
        {
            _logger.LogWarning(
                "Retro skipped: no snapshot for Run={RunId}",
                correction.SourcePayrollRunId);
            return null;
        }

        // 3. Deserialize — snapshot contains all employee inputs frozen at run time
        List<EmployeePayrollInput>? inputs;
        try
        {
            inputs = JsonSerializer.Deserialize<List<EmployeePayrollInput>>(snapshot.SerializedData, JsonOpts);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Retro failed: snapshot deserialization error for Run={RunId}", correction.SourcePayrollRunId);
            return null;
        }

        var originalInput = inputs?.FirstOrDefault(i => i.EmployeeId == correction.EmployeeId);
        if (originalInput is null)
        {
            _logger.LogWarning(
                "Retro skipped: employee {EmployeeId} not found in snapshot for Run={RunId}",
                correction.EmployeeId, correction.SourcePayrollRunId);
            return null;
        }

        // 4. Apply correction: replace hours on the corrected date, mark present if previously absent
        var correctedAttendance = originalInput.AttendanceRecords
            .Select(r => r.Date == correction.CorrectedDate
                ? r with
                {
                    HoursWorked = correction.NewHoursWorked,
                    IsPresent = correction.NewHoursWorked > 0,
                }
                : r)
            .ToList();

        var correctedInput = originalInput with { AttendanceRecords = correctedAttendance };

        // 5. Recalculate with same engine (deterministic)
        EmployeePayrollResult recalculated;
        try
        {
            recalculated = _engine.Calculate(
                correction.TenantId,
                correction.SourcePayrollRunId,
                lockedResult.PeriodName,
                lockedResult.Version,
                lockedResult.SnapshotId,
                correctedInput,
                _taxService);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Retro failed: recalculation error for Employee={EmployeeId}, Run={RunId}",
                correction.EmployeeId, correction.SourcePayrollRunId);
            return null;
        }

        // 6. Diff
        var difference = recalculated.NetPay - lockedResult.NetPay;
        if (difference == 0m)
        {
            _logger.LogInformation(
                "Retro: no financial impact for Employee={EmployeeId} on {Date}. Hours {Old}→{New}",
                correction.EmployeeId, correction.CorrectedDate,
                correction.OldHoursWorked, correction.NewHoursWorked);
            return null;
        }

        // 7. Create retro adjustment
        var reason = $"Attendance correction on {correction.CorrectedDate:yyyy-MM-dd}: " +
                     $"{correction.OldHoursWorked}h → {correction.NewHoursWorked}h. " +
                     $"Net difference ₹{difference:F2}.";

        var retro = RetroPayrollAdjustment.Create(
            correction.TenantId,
            correction.EmployeeId,
            correction.SourcePayrollRunId,
            lockedResult.NetPay,
            recalculated.NetPay,
            reason,
            correction.CorrectedBy);

        _logger.LogInformation(
            "Retro created: Employee={EmployeeId}, Run={RunId}, Diff=₹{Diff:F2}",
            correction.EmployeeId, correction.SourcePayrollRunId, difference);

        return retro;
    }
}
