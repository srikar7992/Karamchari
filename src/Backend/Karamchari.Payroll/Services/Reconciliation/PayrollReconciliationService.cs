using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.Disbursement;
using Karamchari.Payroll.Domain.Reconciliation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Payroll.Services.Reconciliation;

/// <summary>
/// Nightly reconciliation service. Scans payroll data for anomalies.
/// Idempotent: calling twice for same period produces same results.
/// </summary>
public sealed class PayrollReconciliationService
{
    private readonly PayrollDbContext _db;
    private readonly ILogger<PayrollReconciliationService> _logger;

    public PayrollReconciliationService(PayrollDbContext db, ILogger<PayrollReconciliationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ReconciliationJob> RunAsync(
        Guid tenantId,
        string periodName,
        int year,
        int month,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(periodName);
        var job = ReconciliationJob.Start(tenantId, periodName);

        try
        {
            var ledgerEntries = await _db.PayrollLedger
                .AsNoTracking()
                .Where(l => l.Year == year && l.Month == month)
                .ToListAsync(ct);

            var employeeIds = ledgerEntries.Select(l => l.EmployeeId).ToList();

            // 1. Duplicate payout detection
            var duplicates = ledgerEntries
                .GroupBy(l => l.EmployeeId)
                .Where(g => g.Count() > 1);

            foreach (var dup in duplicates)
            {
                job.AddAnomaly(PayrollAnomaly.Detect(
                    AnomalyType.DuplicatePayout,
                    AnomalySeverity.Critical,
                    $"Employee {dup.Key} has {dup.Count()} ledger entries for {periodName}",
                    evidence: $"{{\"employeeId\":\"{dup.Key}\",\"count\":{dup.Count()}}}",
                    employeeId: dup.Key,
                    periodName: periodName));
            }

            // 2. Negative net pay detection
            foreach (var entry in ledgerEntries.Where(l => l.NetPay < 0))
            {
                job.AddAnomaly(PayrollAnomaly.Detect(
                    AnomalyType.NegativeNetPay,
                    AnomalySeverity.Critical,
                    $"Negative net pay {entry.NetPay:C} for employee {entry.EmployeeId}",
                    evidence: $"{{\"netPay\":{entry.NetPay},\"gross\":{entry.MonthlyGross}}}",
                    employeeId: entry.EmployeeId,
                    periodName: periodName));
            }

            // 3. High gross variance detection (>50% change from previous month)
            var previousEntries = await _db.PayrollLedger
                .AsNoTracking()
                .Where(l => employeeIds.Contains(l.EmployeeId)
                    && ((month == 1 && l.Year == year - 1 && l.Month == 12)
                        || (month > 1 && l.Year == year && l.Month == month - 1)))
                .ToListAsync(ct);

            var prevMap = previousEntries.ToDictionary(l => l.EmployeeId);

            foreach (var entry in ledgerEntries)
            {
                if (!prevMap.TryGetValue(entry.EmployeeId, out var prev)) continue;

                if (prev.MonthlyGross <= 0) continue;

                var variance = Math.Abs(entry.MonthlyGross - prev.MonthlyGross) / prev.MonthlyGross;

                if (variance > 0.5m)
                {
                    job.AddAnomaly(PayrollAnomaly.Detect(
                        AnomalyType.GrossVarianceHigh,
                        AnomalySeverity.Warning,
                        $"Gross variance {variance:P0} for employee {entry.EmployeeId} vs previous month",
                        evidence: $"{{\"current\":{entry.MonthlyGross},\"previous\":{prev.MonthlyGross},\"variance\":{variance:F4}}}",
                        employeeId: entry.EmployeeId,
                        periodName: periodName,
                        anomalyScore: (decimal)Math.Min(100.0, (double)variance * 10.0)));
                }
            }

            // 4. Failed disbursement detection
            var failedEntries = await _db.Set<DisbursementBatch>()
                .AsNoTracking()
                .Include(b => b.Entries)
                .Where(b => b.PeriodName == periodName
                    && (b.Status == DisbursementBatchStatus.Failed || b.Status == DisbursementBatchStatus.PartiallyProcessed))
                .ToListAsync(ct);

            foreach (var batch in failedEntries)
            {
                var failedCount = batch.Entries.Count(e => e.Status == DisbursementEntryStatus.Failed);
                if (failedCount > 0)
                {
                    job.AddAnomaly(PayrollAnomaly.Detect(
                        AnomalyType.FailedDisbursement,
                        AnomalySeverity.Critical,
                        $"Batch {batch.Id} has {failedCount} failed disbursement entries",
                        evidence: $"{{\"batchId\":\"{batch.Id}\",\"failedCount\":{failedCount}}}",
                        periodName: periodName));
                }
            }

            job.Complete(ledgerEntries.Count);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Reconciliation completed for {PeriodName}. Employees: {Count}, Anomalies: {TotalAnomalies}",
                    periodName, ledgerEntries.Count, job.TotalAnomalies);
            }
        }
        catch (Exception ex)
        {
            job.Fail(ex.Message);
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Reconciliation failed for {PeriodName}", periodName);
            }
        }

        return job;
    }
}
