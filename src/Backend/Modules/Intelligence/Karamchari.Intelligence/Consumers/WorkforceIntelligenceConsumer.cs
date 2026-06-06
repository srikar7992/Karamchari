using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Core.Contracts.IntegrationEvents.V1;
using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Karamchari.Intelligence.Consumers;

/// <summary>
/// Maintains WorkforceSignalRecords in response to upstream integration events
/// and triggers incremental score recalculation.
///
/// Signal coverage:
///   TimesheetApprovedIntegrationEvent   → OvertimeHours28d proxy (hours above 40h/week)
///   LeaveCancelledIntegrationEvent      → LeaveFrequencyRatio bookkeeping
///   EmployeeOnboardedIntegrationEvent   → Baseline seed (ConsecutiveWorkDays = 0)
///   EmployeeTerminatedIntegrationEvent  → Preserve scores, no new recalculation
///
/// Signals NOT covered by real-time events (nightly job only):
///   ConsecutiveWorkDays      — requires ordered shift history
///   DaysWithoutLeave         — computed from roster + approved leave history
///   LateArrivalsMonthly      — requires finalized attendance records
///   ShiftSwaps30d            — no cross-module integration event published yet
///   HighIntensityShiftRatio  — requires full roster snapshot
///   EmergencyFillIns90d      — sourced from WFP CoverageRisk table
///   PeerAttendanceGap        — requires team-level aggregation
///
/// NOTE: LeaveRequestApprovedIntegrationEvent is NOT consumed here because
/// the V1 contract does not include TenantId. The DaysWithoutLeave signal is
/// populated by the nightly recompute job instead.
/// </summary>
public sealed class WorkforceIntelligenceConsumer :
    IConsumer<TimesheetApprovedIntegrationEvent>,
    IConsumer<LeaveCancelledIntegrationEvent>,
    IConsumer<EmployeeOnboardedIntegrationEvent>,
    IConsumer<EmployeeTerminatedIntegrationEvent>
{
    private readonly WorkforceSignalService _signalService;
    private readonly ILogger<WorkforceIntelligenceConsumer> _logger;

    public WorkforceIntelligenceConsumer(
        WorkforceSignalService signalService,
        ILogger<WorkforceIntelligenceConsumer> logger)
    {
        _signalService = signalService;
        _logger = logger;
    }

    /// <summary>
    /// Extracts overtime signal from approved timesheet.
    /// Standard workweek = 40h. Hours above that = weekly OT proxy.
    /// Nightly job aggregates per-week records into the rolling 28-day window value.
    /// </summary>
    public async Task Consume(ConsumeContext<TimesheetApprovedIntegrationEvent> context)
    {
        var ev = context.Message;
        var ct = context.CancellationToken;

        const decimal standardWeeklyHours = 40m;
        var otThisWeek = Math.Max(0m, ev.TotalHours - standardWeeklyHours);

        await _signalService.UpsertSignalAsync(
            ev.TenantId, ev.EmployeeId,
            WorkforceSignalType.OvertimeHours28d,
            otThisWeek,
            ev.WeekStartDate, ct);

        await _signalService.RecalculateEmployeeAsync(ev.TenantId, ev.EmployeeId, ct);

        _logger.LogDebug("Timesheet {Id}: OT proxy {OT:F1}h for employee {EmpId}",
            ev.TimesheetId, otThisWeek, ev.EmployeeId);
    }

    /// <summary>
    /// Cancelled leave may indicate leave avoidance — no signal change needed;
    /// the DaysWithoutLeave counter continues accumulating. Logged for audit.
    /// </summary>
    public Task Consume(ConsumeContext<LeaveCancelledIntegrationEvent> context)
    {
        _logger.LogDebug("Leave cancelled for employee {EmployeeId} (tenant {TenantId}) — DaysWithoutLeave unchanged",
            context.Message.EmployeeId, context.Message.TenantId);
        return Task.CompletedTask;
    }

    /// <summary>Seeds baseline ConsecutiveWorkDays = 0 for new employees.</summary>
    public async Task Consume(ConsumeContext<EmployeeOnboardedIntegrationEvent> context)
    {
        var ev = context.Message;
        var ct = context.CancellationToken;

        await _signalService.UpsertSignalAsync(
            ev.TenantId, ev.EmployeeId,
            WorkforceSignalType.ConsecutiveWorkDays,
            0m,
            ev.HiredOn, ct);

        _logger.LogDebug("Seeded baseline signals for new employee {EmployeeId}", ev.EmployeeId);
    }

    /// <summary>
    /// Terminated employees: final scores preserved for historical reporting.
    /// No new recalculation after termination.
    /// </summary>
    public Task Consume(ConsumeContext<EmployeeTerminatedIntegrationEvent> context)
    {
        _logger.LogDebug("Employee {EmployeeId} terminated on {Date} — workforce scores frozen",
            context.Message.EmployeeId, context.Message.TerminatedOn);
        return Task.CompletedTask;
    }
}
