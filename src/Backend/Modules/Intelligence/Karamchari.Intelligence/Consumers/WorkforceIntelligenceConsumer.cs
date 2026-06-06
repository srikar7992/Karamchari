using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Core.Contracts.IntegrationEvents.V1;
using Karamchari.Core.Contracts.IntegrationEvents.Workforce;
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
///   TimesheetApprovedIntegrationEvent   â†’ OvertimeHours28d proxy (hours above 40h/week)
///   LeaveCancelledIntegrationEvent      â†’ LeaveFrequencyRatio bookkeeping
///   EmployeeOnboardedIntegrationEvent   â†’ Baseline seed (ConsecutiveWorkDays = 0)
///   EmployeeTerminatedIntegrationEvent  â†’ Preserve scores, no new recalculation
///
/// Signals NOT covered by real-time events (nightly job only):
///   ConsecutiveWorkDays      â€” requires ordered shift history
///   DaysWithoutLeave         â€” computed from roster + approved leave history
///   LateArrivalsMonthly      â€” requires finalized attendance records
///   HighIntensityShiftRatio  â€” requires full roster snapshot
///   EmergencyFillIns90d      â€” sourced from WFP CoverageRisk table
///   PeerAttendanceGap        â€” requires team-level aggregation
///
/// Phase 6.1 additions:
///   ShiftSwapApprovedIntegrationEvent â†’ ShiftSwaps30d signal (real-time)
///   OvertimeRejectedIntegrationEvent  â†’ OvertimeRejections30d signal (real-time)
///
/// NOTE: LeaveRequestApprovedIntegrationEvent is NOT consumed here because
/// the V1 contract does not include TenantId. The DaysWithoutLeave signal is
/// populated by the nightly recompute job instead.
/// </summary>
public sealed class WorkforceIntelligenceConsumer :
    IConsumer<TimesheetApprovedIntegrationEvent>,
    IConsumer<LeaveCancelledIntegrationEvent>,
    IConsumer<EmployeeOnboardedIntegrationEvent>,
    IConsumer<EmployeeTerminatedIntegrationEvent>,
    IConsumer<ShiftSwapApprovedIntegrationEvent>,
    IConsumer<OvertimeRejectedIntegrationEvent>
{
    private readonly WorkforceSignalService _signalService;
    private readonly OutcomeLabelService _outcomeLabelService;
    private readonly ILogger<WorkforceIntelligenceConsumer> _logger;

    public WorkforceIntelligenceConsumer(
        WorkforceSignalService signalService,
        OutcomeLabelService outcomeLabelService,
        ILogger<WorkforceIntelligenceConsumer> logger)
    {
        _signalService = signalService;
        _outcomeLabelService = outcomeLabelService;
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

        await _signalService.RecalculateEmployeeAsync(ev.TenantId, ev.EmployeeId, ct: ct);

        _logger.LogDebug("Timesheet {Id}: OT proxy {OT:F1}h for employee {EmpId}",
            ev.TimesheetId, otThisWeek, ev.EmployeeId);
    }

    /// <summary>
    /// Cancelled leave may indicate leave avoidance â€” no signal change needed;
    /// the DaysWithoutLeave counter continues accumulating. Logged for audit.
    /// </summary>
    public Task Consume(ConsumeContext<LeaveCancelledIntegrationEvent> context)
    {
        _logger.LogDebug("Leave cancelled for employee {EmployeeId} (tenant {TenantId}) â€” DaysWithoutLeave unchanged",
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
    /// Records outcome label for ML training data.
    /// </summary>
    public async Task Consume(ConsumeContext<EmployeeTerminatedIntegrationEvent> context)
    {
        var ev = context.Message;
        var ct = context.CancellationToken;

        await _outcomeLabelService.RecordOutcomeAsync(
            ev.TenantId, ev.EmployeeId,
            Domain.Workforce.WorkforceOutcome.Termination,
            ev.TerminatedOn,
            notes: "Recorded from EmployeeTerminatedIntegrationEvent",
            ct: ct);

        _logger.LogDebug("Employee {EmployeeId} terminated on {Date} â€” workforce scores frozen, outcome label recorded",
            ev.EmployeeId, ev.TerminatedOn);
    }

    /// <summary>
    /// Approved shift swap: increments ShiftSwaps30d proxy for the requesting employee.
    /// The daily rolling count is aggregated by the nightly job; this upserts today's record.
    /// </summary>
    public async Task Consume(ConsumeContext<ShiftSwapApprovedIntegrationEvent> context)
    {
        var ev = context.Message;
        var ct = context.CancellationToken;

        // Load current rolling count, add 1, re-upsert
        var currentCount = await _signalService.GetLatestSignalValueAsync(
            ev.TenantId, ev.RequestingEmployeeId,
            WorkforceSignalType.ShiftSwaps30d, ct);

        await _signalService.UpsertSignalAsync(
            ev.TenantId, ev.RequestingEmployeeId,
            WorkforceSignalType.ShiftSwaps30d,
            currentCount + 1m,
            ev.SwapDate, ct);

        await _signalService.RecalculateEmployeeAsync(ev.TenantId, ev.RequestingEmployeeId, ct: ct);

        _logger.LogDebug("Shift swap approved for employee {EmployeeId} â€” ShiftSwaps30d incremented",
            ev.RequestingEmployeeId);
    }

    /// <summary>
    /// Declined overtime offer: increments OvertimeRejections30d signal for the employee.
    /// </summary>
    public async Task Consume(ConsumeContext<OvertimeRejectedIntegrationEvent> context)
    {
        var ev = context.Message;
        var ct = context.CancellationToken;

        var currentCount = await _signalService.GetLatestSignalValueAsync(
            ev.TenantId, ev.EmployeeId,
            WorkforceSignalType.OvertimeRejections30d, ct);

        await _signalService.UpsertSignalAsync(
            ev.TenantId, ev.EmployeeId,
            WorkforceSignalType.OvertimeRejections30d,
            currentCount + 1m,
            ev.WorkDate, ct);

        await _signalService.RecalculateEmployeeAsync(ev.TenantId, ev.EmployeeId, ct: ct);

        _logger.LogDebug("OT rejected by employee {EmployeeId} ({Hours:F1}h) â€” OvertimeRejections30d incremented",
            ev.EmployeeId, ev.OfferedHours);
    }
}

