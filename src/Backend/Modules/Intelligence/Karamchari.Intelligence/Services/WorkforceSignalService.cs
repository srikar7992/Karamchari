using System.Text.Json;
using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Persistence;
using Karamchari.Intelligence.Services.Scoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Intelligence.Services;

/// <summary>
/// Orchestrates Phase 6 workforce intelligence scoring for a tenant.
///
/// Flow per employee:
///   1. Read latest WorkforceSignalRecords from the DB.
///   2. Build BurnoutSignalInput / AttritionSignalInput.
///   3. Call BurnoutScoreCalculator + AttritionScoreCalculator.
///   4. Upsert WorkforceBurnoutScore + WorkforceAttritionScore.
///   5. Raise WorkforceRecommendations for High/Critical results.
///
/// Called by:
///   - WorkforceIntelligenceRecomputeJob (nightly full pass)
///   - WorkforceIntelligenceConsumer (event-driven incremental)
/// </summary>
public sealed class WorkforceSignalService
{
    private readonly IntelligenceDbContext _db;
    private readonly ILogger<WorkforceSignalService> _logger;

    public WorkforceSignalService(
        IntelligenceDbContext db,
        ILogger<WorkforceSignalService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Recalculates burnout + attrition scores for a single employee.
    /// Called on-demand when a relevant event arrives.
    /// </summary>
    public async Task RecalculateEmployeeAsync(
        string tenantId,
        Guid employeeId,
        IReadOnlyDictionary<WorkforceRecommendationType, decimal>? effectivenessRates = null,
        CancellationToken ct = default)
    {
        var signals = await _db.WorkforceSignalRecords
            .Where(s => s.TenantId == tenantId && s.EmployeeId == employeeId)
            .ToListAsync(ct);

        if (signals.Count == 0)
        {
            _logger.LogDebug("No signals for employee {EmployeeId} — skipping", employeeId);
            return;
        }

        var burnoutInput = BuildBurnoutInput(employeeId, signals);
        var attritionInput = BuildAttritionInput(employeeId, signals);

        await UpsertBurnoutScoreAsync(tenantId, burnoutInput, effectivenessRates, ct);
        await UpsertAttritionScoreAsync(tenantId, attritionInput, effectivenessRates, ct);

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Full tenant recalculation — called nightly by WorkforceIntelligenceRecomputeJob.
    /// Processes all employees in batches of 500 to avoid full-table scans.
    /// Pass <paramref name="effectivenessRates"/> (loaded once per tenant) to rank recommendations.
    /// </summary>
    public async Task RecalculateTenantAsync(
        string tenantId,
        IReadOnlyDictionary<WorkforceRecommendationType, decimal>? effectivenessRates = null,
        CancellationToken ct = default)
    {
        var employeeIds = await _db.WorkforceSignalRecords
            .Where(s => s.TenantId == tenantId)
            .Select(s => s.EmployeeId)
            .Distinct()
            .ToListAsync(ct);

        _logger.LogInformation("Recalculating workforce scores for {Count} employees in tenant {TenantId}",
            employeeIds.Count, tenantId);

        foreach (var batch in employeeIds.Chunk(500))
        {
            foreach (var employeeId in batch)
            {
                try
                {
                    await RecalculateEmployeeAsync(tenantId, employeeId, effectivenessRates, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Score recalculation failed for employee {EmployeeId}", employeeId);
                }
            }
        }
    }

    /// <summary>Returns the most recent value for a signal type, or 0 if none exists.</summary>
    public async Task<decimal> GetLatestSignalValueAsync(
        string tenantId,
        Guid employeeId,
        WorkforceSignalType signalType,
        CancellationToken ct = default)
    {
        return await _db.WorkforceSignalRecords
            .Where(s => s.TenantId == tenantId
                     && s.EmployeeId == employeeId
                     && s.SignalType == signalType)
            .OrderByDescending(s => s.SignalDate)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Upserts a raw signal record for an employee.
    /// Called by event consumers when a new signal measurement arrives.
    /// </summary>
    public async Task UpsertSignalAsync(
        string tenantId,
        Guid employeeId,
        WorkforceSignalType signalType,
        decimal value,
        DateOnly signalDate,
        CancellationToken ct = default)
    {
        var existing = await _db.WorkforceSignalRecords
            .FirstOrDefaultAsync(s => s.TenantId == tenantId
                                   && s.EmployeeId == employeeId
                                   && s.SignalType == signalType
                                   && s.SignalDate == signalDate, ct);

        if (existing != null)
        {
            // Newer value replaces stale one for the same date — handled via remove + re-add
            _db.WorkforceSignalRecords.Remove(existing);
        }

        _db.WorkforceSignalRecords.Add(
            WorkforceSignalRecord.Record(tenantId, employeeId, signalType, value, signalDate));

        await _db.SaveChangesAsync(ct);
    }

    // ── Input builders ────────────────────────────────────────────────────────

    private static BurnoutSignalInput BuildBurnoutInput(
        Guid employeeId,
        List<WorkforceSignalRecord> signals)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thisMonth = today.Month;
        var prevMonth = today.AddMonths(-1).Month;
        var twoMonthsAgo = today.AddMonths(-2).Month;

        decimal Latest(WorkforceSignalType t) =>
            signals.Where(s => s.SignalType == t)
                   .OrderByDescending(s => s.SignalDate)
                   .Select(s => s.Value)
                   .FirstOrDefault();

        int LatestInt(WorkforceSignalType t) => (int)Latest(t);

        int LateArrivalForMonth(int month) =>
            (int)(signals
                .Where(s => s.SignalType == WorkforceSignalType.LateArrivalsMonthly
                         && s.SignalDate.Month == month)
                .OrderByDescending(s => s.SignalDate)
                .Select(s => s.Value)
                .FirstOrDefault());

        var dataAgeDays = signals.Count > 0
            ? (int)(today.ToDateTime(TimeOnly.MinValue) - signals.Min(s => s.SignalDate).ToDateTime(TimeOnly.MinValue)).TotalDays
            : 0;

        return new BurnoutSignalInput(
            EmployeeId: employeeId,
            ConsecutiveWorkDays: LatestInt(WorkforceSignalType.ConsecutiveWorkDays),
            OvertimeHours28d: Latest(WorkforceSignalType.OvertimeHours28d),
            DaysWithoutLeave: LatestInt(WorkforceSignalType.DaysWithoutLeave),
            LateArrivalsCurrentMonth: LateArrivalForMonth(thisMonth),
            LateArrivalsPreviousMonth: LateArrivalForMonth(prevMonth),
            LateArrivalsMonthMinus2: LateArrivalForMonth(twoMonthsAgo),
            ShiftSwaps30d: LatestInt(WorkforceSignalType.ShiftSwaps30d),
            HighIntensityShiftRatio: Latest(WorkforceSignalType.HighIntensityShiftRatio),
            EmergencyFillIns90d: LatestInt(WorkforceSignalType.EmergencyFillIns90d),
            DataAgeDays: dataAgeDays,
            IsNewJoiner: dataAgeDays < 60,
            ShiftCategory: "DayShift" // resolved by consumer when signal is upserted
        );
    }

    private static AttritionSignalInput BuildAttritionInput(
        Guid employeeId,
        List<WorkforceSignalRecord> signals)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        decimal Latest(WorkforceSignalType t) =>
            signals.Where(s => s.SignalType == t)
                   .OrderByDescending(s => s.SignalDate)
                   .Select(s => s.Value)
                   .FirstOrDefault();

        int LatestInt(WorkforceSignalType t) => (int)Latest(t);

        var dataAgeDays = signals.Count > 0
            ? (int)(today.ToDateTime(TimeOnly.MinValue) - signals.Min(s => s.SignalDate).ToDateTime(TimeOnly.MinValue)).TotalDays
            : 0;

        return new AttritionSignalInput(
            EmployeeId: employeeId,
            LateArrivalSlope: Latest(WorkforceSignalType.LateArrivalSlope),
            LeaveFrequencyRatio: Latest(WorkforceSignalType.LeaveFrequencyRatio),
            SickLeaveDays30d: Latest(WorkforceSignalType.SickLeaveDays30d),
            HistoricalSickLeaveBaseline: 1.0m, // default; overwritten when consumer has baseline data
            ShiftSwaps30d: LatestInt(WorkforceSignalType.ShiftSwaps30d),
            OvertimeRejections30d: LatestInt(WorkforceSignalType.OvertimeRejections30d),
            PeerAttendanceGap: Latest(WorkforceSignalType.PeerAttendanceGap),
            ManagerFrictionScore: Latest(WorkforceSignalType.ManagerFrictionScore),
            DataAgeDays: dataAgeDays,
            IsNewJoiner: dataAgeDays < 60
        );
    }

    // ── Upsert helpers ────────────────────────────────────────────────────────

    private async Task UpsertBurnoutScoreAsync(
        string tenantId,
        BurnoutSignalInput input,
        IReadOnlyDictionary<WorkforceRecommendationType, decimal>? effectivenessRates,
        CancellationToken ct)
    {
        var result = BurnoutScoreCalculator.Calculate(input);
        var componentsJson = JsonSerializer.Serialize(result.ComponentScores);

        var existing = await _db.WorkforceBurnoutScores
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.EmployeeId == input.EmployeeId, ct);

        if (existing == null)
        {
            _db.WorkforceBurnoutScores.Add(WorkforceBurnoutScore.Create(
                tenantId, input.EmployeeId,
                result.Score, result.RiskLevel, result.Confidence,
                result.Explanation, componentsJson));
        }
        else
        {
            existing.Refresh(result.Score, result.RiskLevel, result.Confidence, result.Explanation, componentsJson);
        }

        // Append to history for velocity / forecast computation
        _db.WorkforceScoreSnapshots.Add(WorkforceScoreSnapshot.Record(
            tenantId, input.EmployeeId, "Burnout", result.Score, result.RiskLevel));

        // Persist audit trail so managers can challenge any historical score
        var rootCauses = result.ComponentScores
            .OrderByDescending(kv => kv.Value)
            .Select(kv => kv.Key)
            .ToList();
        var signalValues = new Dictionary<string, decimal>
        {
            ["ConsecutiveWorkDays"] = input.ConsecutiveWorkDays,
            ["OvertimeHours28d"] = input.OvertimeHours28d,
            ["DaysWithoutLeave"] = input.DaysWithoutLeave,
            ["LateArrivalsThisMonth"] = input.LateArrivalsCurrentMonth,
            ["ShiftSwaps30d"] = input.ShiftSwaps30d,
            ["HighIntensityRatio"] = input.HighIntensityShiftRatio,
            ["EmergencyFillIns90d"] = input.EmergencyFillIns90d
        };
        _db.ScoreCalculationAudits.Add(ScoreCalculationAudit.Record(
            tenantId, input.EmployeeId, "Burnout",
            result.Score, result.RiskLevel,
            componentsJson,
            JsonSerializer.Serialize(rootCauses),
            JsonSerializer.Serialize(signalValues)));

        // Raise recommendations for High/Critical employees
        if (result.RiskLevel >= WorkforceRiskLevel.High)
        {
            var recs = RecommendationEngine.ForBurnout(result, input, effectivenessRates);
            foreach (var (type, priority, rationale) in recs)
            {
                // Avoid duplicate open recommendations of the same type
                var alreadyOpen = await _db.WorkforceRecommendations
                    .AnyAsync(r => r.TenantId == tenantId
                                && r.EmployeeId == input.EmployeeId
                                && r.Type == type
                                && r.ResolvedAt == null, ct);

                if (!alreadyOpen)
                {
                    _db.WorkforceRecommendations.Add(
                        WorkforceRecommendation.Raise(tenantId, input.EmployeeId, type, priority, rationale, result.Score));
                }
            }
        }
    }

    private async Task UpsertAttritionScoreAsync(
        string tenantId,
        AttritionSignalInput input,
        IReadOnlyDictionary<WorkforceRecommendationType, decimal>? effectivenessRates,
        CancellationToken ct)
    {
        var result = AttritionScoreCalculator.Calculate(input);
        var componentsJson = JsonSerializer.Serialize(result.ComponentScores);

        var existing = await _db.WorkforceAttritionScores
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.EmployeeId == input.EmployeeId, ct);

        if (existing == null)
        {
            _db.WorkforceAttritionScores.Add(WorkforceAttritionScore.Create(
                tenantId, input.EmployeeId,
                result.Score, result.RiskLevel, result.Confidence,
                result.Explanation, componentsJson));
        }
        else
        {
            existing.Refresh(result.Score, result.RiskLevel, result.Confidence, result.Explanation, componentsJson);
        }

        // Append to history for velocity / forecast computation
        _db.WorkforceScoreSnapshots.Add(WorkforceScoreSnapshot.Record(
            tenantId, input.EmployeeId, "Attrition", result.Score, result.RiskLevel));

        // Persist audit trail
        var attritionRootCauses = result.ComponentScores
            .OrderByDescending(kv => kv.Value)
            .Select(kv => kv.Key)
            .ToList();
        var attritionSignalValues = new Dictionary<string, decimal>
        {
            ["LateArrivalSlope"] = input.LateArrivalSlope,
            ["LeaveFrequencyRatio"] = input.LeaveFrequencyRatio,
            ["SickLeaveDays30d"] = input.SickLeaveDays30d,
            ["ShiftSwaps30d"] = input.ShiftSwaps30d,
            ["OvertimeRejections30d"] = input.OvertimeRejections30d,
            ["PeerAttendanceGap"] = input.PeerAttendanceGap,
            ["ManagerFrictionScore"] = input.ManagerFrictionScore
        };
        _db.ScoreCalculationAudits.Add(ScoreCalculationAudit.Record(
            tenantId, input.EmployeeId, "Attrition",
            result.Score, result.RiskLevel,
            JsonSerializer.Serialize(result.ComponentScores),
            JsonSerializer.Serialize(attritionRootCauses),
            JsonSerializer.Serialize(attritionSignalValues)));

        if (result.RiskLevel >= WorkforceRiskLevel.High)
        {
            var recs = RecommendationEngine.ForAttrition(result, input, effectivenessRates);
            foreach (var (type, priority, rationale) in recs)
            {
                var alreadyOpen = await _db.WorkforceRecommendations
                    .AnyAsync(r => r.TenantId == tenantId
                                && r.EmployeeId == input.EmployeeId
                                && r.Type == type
                                && r.ResolvedAt == null, ct);

                if (!alreadyOpen)
                {
                    _db.WorkforceRecommendations.Add(
                        WorkforceRecommendation.Raise(tenantId, input.EmployeeId, type, priority, rationale, result.Score));
                }
            }
        }
    }
}
