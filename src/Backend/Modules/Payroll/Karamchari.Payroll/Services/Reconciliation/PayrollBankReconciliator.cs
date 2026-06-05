using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.Reconciliation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Payroll.Services.Reconciliation;

/// <summary>
/// Reconciles the total net pay from approved EmployeePayrollResults against
/// an expected bank file total before disbursement is authorised.
///
/// This catches the class of bug where:
///   Payroll system says ₹5,42,18,200
///   Bank file generated ₹5,42,17,000
///   Difference = ₹1,200 — silently underpaid
///
/// Call before DisbursementBatch is created. If any anomaly is Critical, halt disbursement.
/// </summary>
public sealed class PayrollBankReconciliator
{
    private readonly PayrollDbContext _db;
    private readonly ILogger<PayrollBankReconciliator> _logger;

    // Tolerance: differences below this threshold are flagged Warning (not Critical).
    // Set to 0 for zero-tolerance environments.
    private const decimal ToleranceAmount = 0m;

    public PayrollBankReconciliator(PayrollDbContext db, ILogger<PayrollBankReconciliator> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Compares payroll system total against the bank file total, validates employee count,
    /// and checks for duplicate bank account numbers within the same run.
    /// </summary>
    /// <param name="tenantId">Tenant scope.</param>
    /// <param name="payrollRunId">The run whose approved results are the source of truth.</param>
    /// <param name="bankFileTotal">Total amount in the generated bank file (sum of all NEFT/RTGS entries).</param>
    /// <param name="bankFileReference">Bank file identifier for audit trail.</param>
    /// <param name="bankFileEmployeeCount">
    /// Number of payment entries in the bank file. Pass null to skip employee-count validation.
    /// </param>
    /// <param name="bankAccountNumbers">
    /// Account numbers present in the bank file. Pass non-null to enable duplicate-account detection.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<ReconciliationJob> ReconcileBankTotalAsync(
        string tenantId,
        Guid payrollRunId,
        decimal bankFileTotal,
        string bankFileReference,
        int? bankFileEmployeeCount = null,
        IReadOnlyList<string>? bankAccountNumbers = null,
        CancellationToken ct = default)
    {
        var job = ReconciliationJob.Start(tenantId, $"BankRecon:{payrollRunId:N}");

        try
        {
            var results = await _db.EmployeePayrollResults
                .AsNoTracking()
                .Where(r => r.PayrollRunId == payrollRunId)
                .Select(r => new { r.EmployeeId, r.NetPay, r.Status })
                .ToListAsync(ct);

            if (results.Count == 0)
            {
                job.AddAnomaly(PayrollAnomaly.Detect(
                    AnomalyType.DuplicatePayout,
                    AnomalySeverity.Critical,
                    $"No EmployeePayrollResults found for PayrollRunId={payrollRunId}",
                    evidence: $"{{\"payrollRunId\":\"{payrollRunId}\"}}",
                    periodName: bankFileReference));
                job.Complete(0);
                return job;
            }

            // Only approved/locked results count — unapproved results mean run not ready
            var unapproved = results.Where(r =>
                r.Status is not (Domain.Results.EmployeePayrollResultStatus.Approved
                    or Domain.Results.EmployeePayrollResultStatus.Locked)).ToList();

            if (unapproved.Count > 0)
            {
                job.AddAnomaly(PayrollAnomaly.Detect(
                    AnomalyType.DuplicatePayout,
                    AnomalySeverity.Critical,
                    $"{unapproved.Count} results not yet approved for Run={payrollRunId}",
                    evidence: $"{{\"unapprovedCount\":{unapproved.Count}}}",
                    periodName: bankFileReference));
            }

            var payrollTotal = results.Sum(r => r.NetPay);
            var payrollEmployeeCount = results.Count;
            var discrepancy = bankFileTotal - payrollTotal;
            var absDiscrepancy = Math.Abs(discrepancy);

            // Check 1: Net pay total vs bank file total
            if (absDiscrepancy > ToleranceAmount)
            {
                var severity = absDiscrepancy > 1m ? AnomalySeverity.Critical : AnomalySeverity.Warning;

                job.AddAnomaly(PayrollAnomaly.Detect(
                    AnomalyType.GrossVarianceHigh,
                    severity,
                    $"Bank file total ₹{bankFileTotal:F2} ≠ payroll total ₹{payrollTotal:F2}. " +
                    $"Discrepancy: ₹{discrepancy:F2}",
                    evidence: $"{{\"bankFileTotal\":{bankFileTotal}," +
                               $"\"payrollTotal\":{payrollTotal}," +
                               $"\"discrepancy\":{discrepancy}," +
                               $"\"bankFileRef\":\"{bankFileReference}\"," +
                               $"\"payrollEmployeeCount\":{payrollEmployeeCount}}}",
                    periodName: bankFileReference,
                    anomalyScore: Math.Min(100m, absDiscrepancy / payrollTotal * 100m)));

                _logger.LogWarning(
                    "Bank reconciliation discrepancy: PayrollRun={RunId}, BankFile={Ref}, " +
                    "PayrollTotal=₹{Payroll:F2}, BankTotal=₹{Bank:F2}, Diff=₹{Diff:F2}",
                    payrollRunId, bankFileReference, payrollTotal, bankFileTotal, discrepancy);
            }
            else
            {
                _logger.LogInformation(
                    "Bank reconciliation passed: PayrollRun={RunId}, Total=₹{Total:F2}, Employees={Count}",
                    payrollRunId, payrollTotal, payrollEmployeeCount);
            }

            // Check 2: Employee count matches bank file entry count
            if (bankFileEmployeeCount.HasValue && bankFileEmployeeCount.Value != payrollEmployeeCount)
            {
                job.AddAnomaly(PayrollAnomaly.Detect(
                    AnomalyType.DuplicatePayout,
                    AnomalySeverity.Critical,
                    $"Employee count mismatch: payroll has {payrollEmployeeCount} results, " +
                    $"bank file has {bankFileEmployeeCount.Value} entries.",
                    evidence: $"{{\"payrollCount\":{payrollEmployeeCount},\"bankFileCount\":{bankFileEmployeeCount.Value}}}",
                    periodName: bankFileReference));

                _logger.LogWarning(
                    "Employee count mismatch: PayrollRun={RunId}, Payroll={P}, BankFile={B}",
                    payrollRunId, payrollEmployeeCount, bankFileEmployeeCount.Value);
            }

            // Check 3: Duplicate bank account numbers within this run's payment set
            if (bankAccountNumbers is { Count: > 0 })
            {
                var duplicates = bankAccountNumbers
                    .GroupBy(a => a, StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicates.Count > 0)
                {
                    var sample = string.Join(", ", duplicates.Take(5));
                    job.AddAnomaly(PayrollAnomaly.Detect(
                        AnomalyType.DuplicatePayout,
                        AnomalySeverity.Critical,
                        $"{duplicates.Count} duplicate bank account(s) in bank file. " +
                        $"Same account would receive multiple payments. Sample: {sample}",
                        evidence: $"{{\"duplicateCount\":{duplicates.Count}," +
                                  $"\"sample\":[{string.Join(",", duplicates.Take(5).Select(d => $"\"{d}\""))}]}}",
                        periodName: bankFileReference));

                    _logger.LogError(
                        "Duplicate bank accounts in PayrollRun={RunId}: {Count} accounts appear multiple times. Sample: {Sample}",
                        payrollRunId, duplicates.Count, sample);
                }
            }

            job.Complete(payrollEmployeeCount);
        }
        catch (Exception ex)
        {
            job.Fail(ex.Message);
            _logger.LogError(ex, "Bank reconciliation failed for PayrollRun={RunId}", payrollRunId);
        }

        return job;
    }
}
