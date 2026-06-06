using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Core.Contracts.IntegrationEvents.V1;
using Karamchari.Forecasting.Domain.Workforce;
using Karamchari.Forecasting.Persistence;
using Karamchari.Forecasting.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Forecasting.Consumers;

/// <summary>
/// Maintains WFP read models and enqueues projections for debounced recomputation.
///
/// Phase 5.1 change: consumers no longer call RecomputeProjectionAsync directly.
/// Instead, they enqueue dirty (tenant, location, skill) keys into WfpRecomputeQueue.
/// RecomputeQueueDrainer flushes every 30 seconds — so 10,000 events for the same projection
/// collapse into a single recompute call, eliminating the fan-out explosion under high event volume.
///
/// Read models maintained:
/// - WorkforceEmployeeIndex — employee → (tenant, location, skills)
/// - WorkforcePlanningProjection — per-(tenant, location, skill) supply state
/// - WfpApprovedLeaveRecord — future approved leaves
/// - WfpSkillExpiry — future skill certification expiry dates
/// - WfpAttendanceSnapshot — daily actual headcount from approved timesheets
/// - WfpAttritionSnapshot — monthly termination counts for rolling attrition rate
/// </summary>
public sealed class WorkforcePlanningConsumer :
    IConsumer<EmployeeOnboardedIntegrationEvent>,
    IConsumer<EmployeeTerminatedIntegrationEvent>,
    IConsumer<SkillAssignedIntegrationEvent>,
    IConsumer<SkillExpiredIntegrationEvent>,
    IConsumer<LeaveRequestApprovedIntegrationEvent>,
    IConsumer<LeaveCancelledIntegrationEvent>,
    IConsumer<TimesheetApprovedIntegrationEvent>,
    IConsumer<HireOfferAcceptedIntegrationEvent>
{
    private const string DefaultSkillCode = "GENERAL";

    private readonly ForecastingDbContext _db;
    private readonly WorkforcePlanningService _wfpService;
    private readonly ILogger<WorkforcePlanningConsumer> _logger;

    public WorkforcePlanningConsumer(
        ForecastingDbContext db,
        WorkforcePlanningService wfpService,
        ILogger<WorkforcePlanningConsumer> logger)
    {
        _db = db;
        _wfpService = wfpService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EmployeeOnboardedIntegrationEvent> context)
    {
        var ev = context.Message;
        var ct = context.CancellationToken;

        var existing = await _db.WorkforceEmployeeIndex.FindAsync([ev.EmployeeId], ct);
        if (existing != null) return; // idempotent

        var index = WorkforceEmployeeIndex.Create(ev.EmployeeId, ev.TenantId);
        index.AssignSkill(DefaultSkillCode);
        if (ev.DateOfBirth.HasValue) index.SetDateOfBirth(ev.DateOfBirth.Value);
        if (ev.ContractEndDate.HasValue) index.SetContractEndDate(ev.ContractEndDate.Value);
        _db.WorkforceEmployeeIndex.Add(index);

        await EnsureProjectionAsync(ev.TenantId, "DEFAULT", DefaultSkillCode, ct);
        await IncrementProjectionAsync(ev.TenantId, "DEFAULT", DefaultSkillCode, ct);

        // Seed WfpContractExpiry for GENERAL skill if this is a fixed-term employee
        if (ev.ContractEndDate.HasValue)
        {
            _db.WfpContractExpiries.Add(
                WfpContractExpiry.Create(ev.EmployeeId, ev.TenantId, "DEFAULT", DefaultSkillCode, ev.ContractEndDate.Value));
        }

        // Seed WfpRetirementRisk for GENERAL skill if DateOfBirth is known
        if (ev.DateOfBirth.HasValue)
        {
            var retirementAge = await GetRetirementAgeAsync(ev.TenantId, null, ct);
            var retirementDate = ev.DateOfBirth.Value.AddYears(retirementAge);
            if (retirementDate > DateOnly.FromDateTime(DateTime.UtcNow))
            {
                _db.WfpRetirementRisks.Add(
                    WfpRetirementRisk.Create(ev.EmployeeId, ev.TenantId, "DEFAULT", DefaultSkillCode, retirementDate));
            }
        }

        // Mark one matching future hire as onboarded (for (tenant, DEFAULT, GENERAL) window)
        var futureHire = await _db.WfpFutureHires
            .Where(h => h.TenantId == ev.TenantId &&
                        h.LocationCode == "DEFAULT" &&
                        h.SkillCode == DefaultSkillCode &&
                        !h.IsOnboarded &&
                        h.ExpectedStartDate <= DateOnly.FromDateTime(DateTime.UtcNow))
            .OrderBy(h => h.ExpectedStartDate)
            .FirstOrDefaultAsync(ct);
        futureHire?.MarkOnboarded();

        await _db.SaveChangesAsync(ct);

        await _wfpService.EnqueueRecomputeAsync(ev.TenantId, "DEFAULT", DefaultSkillCode, ct);

        _logger.LogDebug("WFP: indexed new employee {EmployeeId} for tenant {TenantId}", ev.EmployeeId, ev.TenantId);
    }

    public async Task Consume(ConsumeContext<EmployeeTerminatedIntegrationEvent> context)
    {
        var ev = context.Message;
        var ct = context.CancellationToken;

        var index = await _db.WorkforceEmployeeIndex.FindAsync([ev.EmployeeId], ct);
        if (index == null || !index.IsActive) return;

        var locationCode = index.LocationCode;
        var skillCodes = index.SkillCodes.Count > 0 ? index.SkillCodes.ToList() : [DefaultSkillCode];

        index.Terminate();

        foreach (var skill in skillCodes)
        {
            await DecrementProjectionAsync(ev.TenantId, locationCode, skill, ct);

            var now = DateTime.UtcNow;
            var attrSnap = await _db.WfpAttritionSnapshots
                .FirstOrDefaultAsync(a =>
                    a.TenantId == ev.TenantId &&
                    a.LocationCode == locationCode &&
                    a.SkillCode == skill &&
                    a.Year == now.Year &&
                    a.Month == now.Month,
                    ct);

            if (attrSnap == null)
            {
                attrSnap = WfpAttritionSnapshot.Create(ev.TenantId, locationCode, skill, now.Year, now.Month);
                _db.WfpAttritionSnapshots.Add(attrSnap);
            }
            attrSnap.IncrementTerminations();
        }

        var expiryRecords = await _db.WfpSkillExpiries
            .Where(e => e.EmployeeId == ev.EmployeeId && !e.IsRevoked)
            .ToListAsync(ct);
        foreach (var expiry in expiryRecords)
            expiry.Revoke();

        // Mark contract expiries and retirement risks as terminated (they've already left)
        var contractExpiries = await _db.WfpContractExpiries
            .Where(c => c.EmployeeId == ev.EmployeeId && !c.IsTerminated)
            .ToListAsync(ct);
        foreach (var contract in contractExpiries)
            contract.MarkTerminated();

        var retirementRisks = await _db.WfpRetirementRisks
            .Where(r => r.EmployeeId == ev.EmployeeId && !r.IsRetired)
            .ToListAsync(ct);
        foreach (var retirement in retirementRisks)
            retirement.MarkRetired();

        await _db.SaveChangesAsync(ct);

        // Refresh attrition rate (seasonal-aware blend) then queue recomputes
        foreach (var skill in skillCodes)
        {
            await _wfpService.RefreshAttritionRateAsync(ev.TenantId, locationCode, skill, ct);
            await _wfpService.EnqueueRecomputeAsync(ev.TenantId, locationCode, skill, ct);
        }

        _logger.LogDebug("WFP: terminated employee {EmployeeId} for tenant {TenantId}", ev.EmployeeId, ev.TenantId);
    }

    public async Task Consume(ConsumeContext<SkillAssignedIntegrationEvent> context)
    {
        var ev = context.Message;
        var ct = context.CancellationToken;

        var index = await _db.WorkforceEmployeeIndex.FindAsync([ev.EmployeeId], ct);
        if (index == null || !index.IsActive) return;

        var alreadyHadSkill = index.SkillCodes.Contains(ev.SkillCode);
        index.AssignSkill(ev.SkillCode);

        if (!alreadyHadSkill)
        {
            await EnsureProjectionAsync(ev.TenantId, index.LocationCode, ev.SkillCode, ct);
            await IncrementProjectionAsync(ev.TenantId, index.LocationCode, ev.SkillCode, ct);
        }

        if (ev.ExpiryDate.HasValue)
        {
            var existingExpiry = await _db.WfpSkillExpiries
                .FirstOrDefaultAsync(e => e.EmployeeId == ev.EmployeeId && e.SkillCode == ev.SkillCode && !e.IsRevoked, ct);

            if (existingExpiry == null)
            {
                _db.WfpSkillExpiries.Add(WfpSkillExpiry.Create(
                    ev.EmployeeId, ev.TenantId, index.LocationCode, ev.SkillCode, ev.ExpiryDate.Value));
            }
            else
            {
                existingExpiry.RenewExpiry(ev.ExpiryDate.Value);
            }
        }

        // Propagate contract expiry to the newly assigned skill
        if (index.ContractEndDate.HasValue)
        {
            var contractExists = await _db.WfpContractExpiries.AnyAsync(
                c => c.EmployeeId == ev.EmployeeId && c.SkillCode == ev.SkillCode && !c.IsTerminated, ct);
            if (!contractExists)
            {
                _db.WfpContractExpiries.Add(
                    WfpContractExpiry.Create(ev.EmployeeId, ev.TenantId, index.LocationCode, ev.SkillCode, index.ContractEndDate.Value));
            }
        }

        // Propagate retirement risk to the newly assigned skill
        if (index.DateOfBirth.HasValue)
        {
            var retirementAge = await GetRetirementAgeAsync(ev.TenantId, null, ct);
            var retirementDate = index.DateOfBirth.Value.AddYears(retirementAge);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (retirementDate > today)
            {
                var retirementExists = await _db.WfpRetirementRisks.AnyAsync(
                    r => r.EmployeeId == ev.EmployeeId && r.SkillCode == ev.SkillCode && !r.IsRetired, ct);
                if (!retirementExists)
                {
                    _db.WfpRetirementRisks.Add(
                        WfpRetirementRisk.Create(ev.EmployeeId, ev.TenantId, index.LocationCode, ev.SkillCode, retirementDate));
                }
            }
        }

        await _db.SaveChangesAsync(ct);
        await _wfpService.EnqueueRecomputeAsync(ev.TenantId, index.LocationCode, ev.SkillCode, ct);
    }

    public async Task Consume(ConsumeContext<SkillExpiredIntegrationEvent> context)
    {
        var ev = context.Message;
        var ct = context.CancellationToken;

        var index = await _db.WorkforceEmployeeIndex.FindAsync([ev.EmployeeId], ct);
        if (index == null || !index.IsActive) return;

        var hadSkill = index.SkillCodes.Contains(ev.SkillCode);
        index.RevokeSkill(ev.SkillCode);

        if (hadSkill)
            await DecrementProjectionAsync(ev.TenantId, index.LocationCode, ev.SkillCode, ct);

        var expiryRecord = await _db.WfpSkillExpiries
            .FirstOrDefaultAsync(e => e.EmployeeId == ev.EmployeeId && e.SkillCode == ev.SkillCode && !e.IsRevoked, ct);
        expiryRecord?.Revoke();

        await _db.SaveChangesAsync(ct);
        await _wfpService.EnqueueRecomputeAsync(ev.TenantId, index.LocationCode, ev.SkillCode, ct);
    }

    public async Task Consume(ConsumeContext<LeaveRequestApprovedIntegrationEvent> context)
    {
        var ev = context.Message;
        var ct = context.CancellationToken;

        var existing = await _db.WfpApprovedLeaveRecords
            .FirstOrDefaultAsync(r => r.RequestId == ev.RequestId, ct);
        if (existing != null) return;

        var index = await _db.WorkforceEmployeeIndex.FindAsync([ev.EmployeeId], ct);
        if (index == null || !index.IsActive) return;

        var skillCodes = index.SkillCodes.Count > 0 ? index.SkillCodes : [DefaultSkillCode];

        foreach (var skillCode in skillCodes)
        {
            _db.WfpApprovedLeaveRecords.Add(WfpApprovedLeaveRecord.Create(
                ev.RequestId, index.TenantId, ev.EmployeeId,
                index.LocationCode, skillCode, ev.StartDate, ev.EndDate));
        }

        await _db.SaveChangesAsync(ct);

        foreach (var skillCode in skillCodes)
            await _wfpService.EnqueueRecomputeAsync(index.TenantId, index.LocationCode, skillCode, ct);
    }

    public async Task Consume(ConsumeContext<LeaveCancelledIntegrationEvent> context)
    {
        var ev = context.Message;
        var ct = context.CancellationToken;

        var records = await _db.WfpApprovedLeaveRecords
            .Where(r => r.RequestId == ev.RequestId && !r.IsCancelled)
            .ToListAsync(ct);

        foreach (var record in records)
            record.Cancel();

        if (records.Count == 0) return;

        await _db.SaveChangesAsync(ct);

        var affectedSkills = records.Select(r => r.SkillCode).Distinct().ToList();
        var tenantId = records[0].TenantId;
        var locationCode = records[0].LocationCode;

        foreach (var skill in affectedSkills)
            await _wfpService.EnqueueRecomputeAsync(tenantId, locationCode, skill, ct);
    }

    public async Task Consume(ConsumeContext<TimesheetApprovedIntegrationEvent> context)
    {
        var ev = context.Message;
        var ct = context.CancellationToken;

        var index = await _db.WorkforceEmployeeIndex.FindAsync([ev.EmployeeId], ct);
        if (index == null || !index.IsActive) return;

        var skillCodes = index.SkillCodes.Count > 0 ? index.SkillCodes : [DefaultSkillCode];

        var workedDates = ev.Entries
            .Where(e => e.Hours > 0)
            .Select(e => e.Date)
            .Distinct()
            .ToList();

        foreach (var date in workedDates)
        {
            foreach (var skillCode in skillCodes)
            {
                var snapshot = await _db.WfpAttendanceSnapshots
                    .FirstOrDefaultAsync(s =>
                        s.TenantId == index.TenantId &&
                        s.LocationCode == index.LocationCode &&
                        s.SkillCode == skillCode &&
                        s.Date == date,
                        ct);

                if (snapshot == null)
                {
                    var proj = await _db.WorkforcePlanningProjections
                        .FirstOrDefaultAsync(p =>
                            p.TenantId == index.TenantId &&
                            p.LocationCode == index.LocationCode &&
                            p.SkillCode == skillCode,
                            ct);

                    snapshot = WfpAttendanceSnapshot.Create(
                        index.TenantId, index.LocationCode, skillCode, date,
                        proj?.TotalEmployees ?? 1);
                    _db.WfpAttendanceSnapshots.Add(snapshot);

                    proj?.IncrementDataPoints();
                }

                snapshot.IncrementActual();
            }
        }

        await _db.SaveChangesAsync(ct);

        // Refresh composite DayOfWeek+Month absence rates from accumulated snapshots
        foreach (var skillCode in skillCodes)
        {
            await _wfpService.RefreshAbsenceRatesAsync(
                index.TenantId, index.LocationCode, skillCode, ct);
        }

        // Timesheets don't affect headcount directly — no recompute needed unless absence rates changed significantly.
        // Absence rate refresh in the line above already saves updated rates; nightly job picks them up.
    }

    public async Task Consume(ConsumeContext<HireOfferAcceptedIntegrationEvent> context)
    {
        var ev = context.Message;
        var ct = context.CancellationToken;

        var existing = await _db.WfpFutureHires.AnyAsync(h => h.OfferId == ev.OfferId, ct);
        if (existing) return; // idempotent

        await EnsureProjectionAsync(ev.TenantId, ev.LocationCode, ev.SkillCode, ct);
        _db.WfpFutureHires.Add(WfpFutureHire.Create(ev.OfferId, ev.TenantId, ev.LocationCode, ev.SkillCode, ev.ExpectedStartDate, ev.JoiningProbability));
        await _db.SaveChangesAsync(ct);

        await _wfpService.EnqueueRecomputeAsync(ev.TenantId, ev.LocationCode, ev.SkillCode, ct);

        _logger.LogDebug(
            "WFP: future hire queued for offer {OfferId} tenant {TenantId} starting {StartDate}",
            ev.OfferId, ev.TenantId, ev.ExpectedStartDate);
    }

    // ── Retirement policy helper ──────────────────────────────────────────────

    private async Task<int> GetRetirementAgeAsync(string tenantId, string? roleCode, CancellationToken ct)
    {
        // Role-specific policy first, then tenant default, then global fallback
        if (roleCode != null)
        {
            var rolePolicy = await _db.WfpRetirementPolicies
                .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.RoleCode == roleCode && p.IsActive, ct);
            if (rolePolicy != null) return rolePolicy.RetirementAge;
        }

        var defaultPolicy = await _db.WfpRetirementPolicies
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.RoleCode == null && p.IsActive, ct);
        return defaultPolicy?.RetirementAge ?? WfpRetirementPolicy.FallbackRetirementAge;
    }

    // ── Projection helpers ────────────────────────────────────────────────────

    private async Task EnsureProjectionAsync(string tenantId, string locationCode, string skillCode, CancellationToken ct)
    {
        var exists = await _db.WorkforcePlanningProjections.AnyAsync(
            p => p.TenantId == tenantId && p.LocationCode == locationCode && p.SkillCode == skillCode, ct);

        if (!exists)
            _db.WorkforcePlanningProjections.Add(WorkforcePlanningProjection.Create(tenantId, locationCode, skillCode));
    }

    private async Task IncrementProjectionAsync(string tenantId, string locationCode, string skillCode, CancellationToken ct)
    {
        // Check local tracker first: EnsureProjectionAsync may have Added (not yet saved) the projection
        // in the same unit of work. A DB query would miss it because it hasn't been persisted yet.
        var proj = _db.WorkforcePlanningProjections.Local
            .FirstOrDefault(p => p.TenantId == tenantId && p.LocationCode == locationCode && p.SkillCode == skillCode)
            ?? await _db.WorkforcePlanningProjections
                .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.LocationCode == locationCode && p.SkillCode == skillCode, ct);
        proj?.IncrementHeadcount();
    }

    private async Task DecrementProjectionAsync(string tenantId, string locationCode, string skillCode, CancellationToken ct)
    {
        var proj = _db.WorkforcePlanningProjections.Local
            .FirstOrDefault(p => p.TenantId == tenantId && p.LocationCode == locationCode && p.SkillCode == skillCode)
            ?? await _db.WorkforcePlanningProjections
                .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.LocationCode == locationCode && p.SkillCode == skillCode, ct);
        proj?.DecrementHeadcount();
    }
}
