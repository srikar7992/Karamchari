using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Persistence;
using Karamchari.Intelligence.Services.Scoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Karamchari.Intelligence.Services;

/// <summary>
/// Nightly background job that captures a daily feature snapshot for every employee.
/// Runs at 03:00 UTC (after WorkforceIntelligenceRecomputeJob at 02:00 UTC).
/// Snapshots become the ML training dataset for future supervised models.
/// </summary>
public sealed class FeatureSnapshotJob : BackgroundService
{
    private static readonly TimeOnly RunAt = new(3, 0, 0);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FeatureSnapshotJob> _logger;

    public FeatureSnapshotJob(
        IServiceScopeFactory scopeFactory,
        ILogger<FeatureSnapshotJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = TimeOnly.FromDateTime(DateTime.UtcNow);
            var delay = now < RunAt
                ? RunAt - now
                : TimeSpan.FromHours(24) - (now - RunAt);

            await Task.Delay(delay, stoppingToken);
            if (stoppingToken.IsCancellationRequested) break;

            await CaptureAllSnapshotsAsync(stoppingToken);
        }
    }

    private async Task CaptureAllSnapshotsAsync(CancellationToken ct)
    {
        _logger.LogInformation("FeatureSnapshotJob starting daily capture at {Time:u}", DateTime.UtcNow);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IntelligenceDbContext>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var tenantIds = await db.WorkforceSignalRecords
            .Select(s => s.TenantId)
            .Distinct()
            .ToListAsync(ct);

        int captured = 0;
        foreach (var tenantId in tenantIds)
        {
            var employeeIds = await db.WorkforceSignalRecords
                .Where(s => s.TenantId == tenantId)
                .Select(s => s.EmployeeId)
                .Distinct()
                .ToListAsync(ct);

            foreach (var employeeId in employeeIds)
            {
                try
                {
                    await CaptureEmployeeSnapshotAsync(db, tenantId, employeeId, today, ct);
                    captured++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Snapshot failed for employee {EmployeeId}", employeeId);
                }
            }

            await db.SaveChangesAsync(ct);
        }

        _logger.LogInformation("FeatureSnapshotJob complete — {Count} snapshots captured", captured);
    }

    private static async Task CaptureEmployeeSnapshotAsync(
        IntelligenceDbContext db,
        string tenantId,
        Guid employeeId,
        DateOnly today,
        CancellationToken ct)
    {
        // Skip if already captured today
        var alreadyCaptured = await db.WorkforceFeatureSnapshots
            .AnyAsync(s => s.TenantId == tenantId
                        && s.EmployeeId == employeeId
                        && s.SnapshotDate == today, ct);
        if (alreadyCaptured) return;

        var signals = await db.WorkforceSignalRecords
            .Where(s => s.TenantId == tenantId && s.EmployeeId == employeeId)
            .ToListAsync(ct);

        if (signals.Count == 0) return;

        decimal Latest(WorkforceSignalType t) =>
            signals.Where(s => s.SignalType == t)
                   .OrderByDescending(s => s.SignalDate)
                   .Select(s => s.Value)
                   .FirstOrDefault();

        var burnoutScore = await db.WorkforceBurnoutScores
            .Where(s => s.TenantId == tenantId && s.EmployeeId == employeeId)
            .Select(s => s.Score)
            .FirstOrDefaultAsync(ct);

        var burnoutLevel = await db.WorkforceBurnoutScores
            .Where(s => s.TenantId == tenantId && s.EmployeeId == employeeId)
            .Select(s => s.RiskLevel)
            .FirstOrDefaultAsync(ct);

        var attritionScore = await db.WorkforceAttritionScores
            .Where(s => s.TenantId == tenantId && s.EmployeeId == employeeId)
            .Select(s => s.Score)
            .FirstOrDefaultAsync(ct);

        var attritionLevel = await db.WorkforceAttritionScores
            .Where(s => s.TenantId == tenantId && s.EmployeeId == employeeId)
            .Select(s => s.RiskLevel)
            .FirstOrDefaultAsync(ct);

        var dependency = Latest(WorkforceSignalType.CriticalShiftCoverageRatio);
        var talentInput = new TalentRiskInput(employeeId, burnoutScore, attritionScore, dependency);
        var talentResult = TalentRiskCalculator.Calculate(talentInput);

        db.WorkforceFeatureSnapshots.Add(WorkforceFeatureSnapshot.Capture(
            tenantId, employeeId, today,
            consecutiveWorkDays: (int)Latest(WorkforceSignalType.ConsecutiveWorkDays),
            overtimeHours28d: Latest(WorkforceSignalType.OvertimeHours28d),
            daysWithoutLeave: (int)Latest(WorkforceSignalType.DaysWithoutLeave),
            lateArrivalsCurrentMonth: (int)Latest(WorkforceSignalType.LateArrivalsMonthly),
            shiftSwaps30d: (int)Latest(WorkforceSignalType.ShiftSwaps30d),
            highIntensityShiftRatio: Latest(WorkforceSignalType.HighIntensityShiftRatio),
            emergencyFillIns90d: (int)Latest(WorkforceSignalType.EmergencyFillIns90d),
            lateArrivalSlope: Latest(WorkforceSignalType.LateArrivalSlope),
            leaveFrequencyRatio: Latest(WorkforceSignalType.LeaveFrequencyRatio),
            sickLeaveDays30d: Latest(WorkforceSignalType.SickLeaveDays30d),
            overtimeRejections30d: (int)Latest(WorkforceSignalType.OvertimeRejections30d),
            peerAttendanceGap: Latest(WorkforceSignalType.PeerAttendanceGap),
            managerFrictionScore: Latest(WorkforceSignalType.ManagerFrictionScore),
            burnoutScore: burnoutScore,
            attritionScore: attritionScore,
            talentRiskScore: talentResult.Score,
            burnoutRiskLevel: burnoutLevel,
            attritionRiskLevel: attritionLevel));
    }
}
