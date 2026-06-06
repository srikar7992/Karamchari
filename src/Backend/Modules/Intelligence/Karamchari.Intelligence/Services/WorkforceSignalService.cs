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

        await UpsertBurnoutScoreAsync(tenantId, burnoutInput, ct);
        await UpsertAttritionScoreAsync(tenantId, attritionInput, ct);

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Full tenant recalculation — called nightly by WorkforceIntelligenceRecomputeJob.
    /// Processes all employees in batches of 500 to avoid full-table scans.
    /// </summary>
    public async Task RecalculateTenantAsync(string tenantId, CancellationToken ct = default)
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
                    await RecalculateEmployeeAsync(tenantId, employeeId, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Score recalculation failed for employee {EmployeeId}", employeeId);
                }
            }
        }
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

        // Raise recommendations for High/Critical employees
        if (result.RiskLevel >= WorkforceRiskLevel.High)
        {
            var recs = RecommendationEngine.ForBurnout(result, input);
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

        if (result.RiskLevel >= WorkforceRiskLevel.High)
        {
            var recs = RecommendationEngine.ForAttrition(result, input);
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
