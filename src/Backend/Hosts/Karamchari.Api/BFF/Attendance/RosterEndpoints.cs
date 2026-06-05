using System.Security.Claims;
using Karamchari.Api.BFF.Common;
using Karamchari.Api.Middleware;
using Karamchari.Core.Multitenancy;
using Karamchari.TimeAttendance.Domain.Rostering;
using Karamchari.TimeAttendance.Domain.Rostering.Constraints;
using Karamchari.TimeAttendance.Domain.Rostering.Scheduling;
using Karamchari.TimeAttendance.Domain.Rostering.Validation;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Attendance;

public static class RosterEndpoints
{
    /// <summary>Marker type for ILogger category. Required because static classes cannot be type arguments.</summary>
    private sealed class Log { }
    public static WebApplication MapRosterEndpoints(this WebApplication app)
    {
        var rosters = app.MapGroup("/api/v1/rosters").RequireAuthorization();
        var assignments = app.MapGroup("/api/v1/assignments").RequireAuthorization();
        var swaps = app.MapGroup("/api/v1/swaps").RequireAuthorization();
        var coverage = app.MapGroup("/api/v1/coverage-rules").RequireAuthorization();
        var skills = app.MapGroup("/api/v1/employee-skills").RequireAuthorization();

        // Roster lifecycle
        rosters.MapPost("/", CreateRoster).WithName("Roster.Create").WithIdempotency();
        rosters.MapGet("/{id:guid}", GetRoster).WithName("Roster.Get");
        rosters.MapGet("/", ListRosters).WithName("Roster.List");
        rosters.MapPost("/{id:guid}/review", MoveRosterToReview).WithName("Roster.Review");
        rosters.MapPost("/{id:guid}/publish", PublishRoster).WithName("Roster.Publish");
        rosters.MapPost("/{id:guid}/archive", ArchiveRoster).WithName("Roster.Archive");

        // Shifts within a roster
        rosters.MapPost("/{id:guid}/shifts", CreateShift).WithName("Roster.Shift.Create");
        rosters.MapGet("/{id:guid}/shifts", ListShifts).WithName("Roster.Shift.List");
        rosters.MapPost("/{id:guid}/shifts/{shiftId:guid}/auto-assign", AutoAssignShift).WithName("Roster.Shift.AutoAssign");
        rosters.MapPost("/{id:guid}/auto-fill", AutoFillRoster).WithName("Roster.AutoFill");

        // Assignments
        assignments.MapPost("/", CreateAssignment).WithName("Assignment.Create").WithIdempotency();
        assignments.MapDelete("/{id:guid}", RemoveAssignment).WithName("Assignment.Remove");

        // Swaps
        swaps.MapPost("/request", RequestSwap).WithName("Swap.Request").WithIdempotency();
        swaps.MapPost("/{id:guid}/accept", AcceptSwap).WithName("Swap.Accept");
        swaps.MapPost("/{id:guid}/approve", ApproveSwap).WithName("Swap.Approve");
        swaps.MapPost("/{id:guid}/reject", RejectSwap).WithName("Swap.Reject");
        swaps.MapPost("/{id:guid}/cancel", CancelSwap).WithName("Swap.Cancel");

        // Coverage rules
        coverage.MapPost("/", CreateCoverageRule).WithName("Coverage.Create").WithIdempotency();
        coverage.MapGet("/", ListCoverageRules).WithName("Coverage.List");
        coverage.MapDelete("/{id:guid}", DeactivateCoverageRule).WithName("Coverage.Deactivate");

        // Employee skills
        skills.MapPost("/", GrantEmployeeSkill).WithName("EmployeeSkill.Grant").WithIdempotency();
        skills.MapGet("/{employeeId:guid}", GetEmployeeSkills).WithName("EmployeeSkill.List");
        skills.MapDelete("/{id:guid}", RevokeEmployeeSkill).WithName("EmployeeSkill.Revoke");

        // Employee availability preferences
        var availability = app.MapGroup("/api/v1/employee-availability").RequireAuthorization();
        availability.MapPost("/", DeclareAvailability).WithName("Availability.Declare").WithIdempotency();
        availability.MapGet("/{employeeId:guid}", GetAvailability).WithName("Availability.Get");
        availability.MapDelete("/{id:guid}", RevokeAvailability).WithName("Availability.Revoke");

        return app;
    }

    // ── Roster ────────────────────────────────────────────────────────────────────

    private static async Task<IResult> CreateRoster(
        [FromBody] CreateRosterRequest request,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var roster = Roster.Create(tenantId, request.Name, request.StartDate, request.EndDate);
        db.Rosters.Add(roster);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/rosters/{roster.Id}", new { roster.Id });
    }

    private static async Task<IResult> GetRoster(
        Guid id,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var roster = await db.Rosters
            .Include(r => r.Shifts)
            .Include(r => r.Assignments)
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, ct);

        return roster is null ? Results.NotFound() : Results.Ok(roster);
    }

    private static async Task<IResult> ListRosters(
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        [FromQuery] RosterStatus? status,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var query = db.Rosters.Where(r => r.TenantId == tenantId);
        if (status.HasValue) query = query.Where(r => r.Status == status.Value);

        var rosters = await query
            .OrderByDescending(r => r.StartDate)
            .Select(r => new { r.Id, r.Name, r.StartDate, r.EndDate, r.Status, r.PublishedAt })
            .ToListAsync(ct);

        return Results.Ok(rosters);
    }

    private static async Task<IResult> MoveRosterToReview(
        Guid id,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var roster = await db.Rosters.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, ct);
        if (roster is null) return Results.NotFound();

        roster.MoveToReview();
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> PublishRoster(
        Guid id,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        ILogger<Log> logger,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        // Serializable isolation: prevents phantom reads between validate and commit.
        // Blocks a concurrent ApproveLeave from inserting a leave row for any employee
        // whose leave data we've read during validation — closing the validate-then-publish race window.
        await using var tx = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, ct);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var roster = await db.Rosters
                .Include(r => r.Shifts)
                .Include(r => r.Assignments)
                .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, ct);
            if (roster is null) return Results.NotFound();

            var shifts = roster.Shifts.ToList();
            var coverageRules = await db.CoverageRules
                .Where(c => c.TenantId == tenantId && c.IsActive)
                .ToListAsync(ct);

            var employeeIds = roster.Assignments
                .Where(a => a.Status != AssignmentStatus.Cancelled)
                .Select(a => a.EmployeeId)
                .Distinct()
                .ToList();

            // TODO: rosterStart is used as the context date — leaves in weeks 2+ of a multi-week
            // roster are not loaded. Correct fix: load leaves for the full [rosterStart, rosterEnd] range.
            var employeeContexts = await AssignmentContextLoader.BulkAsync(
                employeeIds, tenantId, roster.StartDate, db, ct);

            var validation = RosterPublishValidator.Validate(roster, shifts, coverageRules, employeeContexts);
            if (!validation.IsValid)
            {
                sw.Stop();
                logger.LogWarning(
                    "PublishRoster {RosterId}: validation failed with {ViolationCount} violations in {Ms}ms.",
                    id, validation.Violations.Count, sw.ElapsedMilliseconds);
                return Results.UnprocessableEntity(new
                {
                    Message = "Roster cannot be published. Resolve all violations first.",
                    Violations = validation.Violations.Select(v => new
                    {
                        v.Code,
                        v.Description,
                        Date = v.Date?.ToString("yyyy-MM-dd"),
                        ShiftId = v.ShiftId?.ToString(),
                    }),
                });
            }

            roster.Publish();
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            sw.Stop();
            logger.LogInformation(
                "PublishRoster {RosterId}: published {Assignments} assignments in {Ms}ms.",
                id, employeeIds.Count, sw.ElapsedMilliseconds);
            return Results.NoContent();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            sw.Stop();
            logger.LogWarning("PublishRoster {RosterId}: concurrency conflict after {Ms}ms. {Message}", id, sw.ElapsedMilliseconds, ex.Message);
            return Results.Conflict(new
            {
                Message = "Roster was modified concurrently during publish. Reload and retry.",
                Code = "CONCURRENCY_CONFLICT",
            });
        }
    }

    private static async Task<IResult> ArchiveRoster(
        Guid id,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var roster = await db.Rosters.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, ct);
        if (roster is null) return Results.NotFound();

        roster.Archive();
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    // ── Shifts ────────────────────────────────────────────────────────────────────

    private static async Task<IResult> CreateShift(
        Guid id,
        [FromBody] CreateShiftRequest request,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var roster = await db.Rosters
            .Include(r => r.Shifts)
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, ct);
        if (roster is null) return Results.NotFound();

        var shift = roster.AddShift(
            request.Name,
            request.Type,
            request.WorkDate,
            request.StartTime,
            request.EndTime,
            request.LocationId,
            request.RequiredHeadcount,
            request.RequiredSkills?.Select(s => new SkillRequirement(s.SkillId, s.SkillName, s.MinimumLevel, s.Count)));

        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/rosters/{id}/shifts/{shift.Id}", new { shift.Id });
    }

    private static async Task<IResult> ListShifts(
        Guid id,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var shifts = await db.RosterShifts
            .Where(s => s.RosterId == id && s.TenantId == tenantId)
            .OrderBy(s => s.WorkDate).ThenBy(s => s.StartTime)
            .ToListAsync(ct);

        return Results.Ok(shifts);
    }

    /// <summary>
    /// Auto-assigns the best eligible candidates to a shift based on constraint engine + scorer.
    /// Fills up to RequiredHeadcount slots. Returns list of created assignment IDs and any
    /// candidates that were skipped (with constraint violation reason).
    /// <para>
    /// Strategy: <b>greedy per-shift assignment</b> — score candidates, validate top-N against hard
    /// constraints, assign until slots are filled. Does NOT optimize across multiple shifts
    /// simultaneously. Callers must not assume global optimality; a greedy schedule may leave a
    /// later shift under-staffed if a highly-qualified candidate was consumed here first.
    /// Use <c>POST /rosters/{id}/auto-fill</c> (not yet implemented) for multi-shift optimization.
    /// </para>
    /// </summary>
    private static async Task<IResult> AutoAssignShift(
        Guid id,
        Guid shiftId,
        [FromBody] AutoAssignRequest request,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        ILogger<Log> logger,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        // Partial load: avoid loading all shifts/assignments for potentially large rosters.
        // Only load the target shift and assignments for that specific shift.
        var roster = await db.Rosters
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, ct);
        if (roster is null) return Results.NotFound("Roster not found.");

        await db.Entry(roster).Collection(r => r.Shifts)
            .Query()
            .Where(s => s.Id == shiftId)
            .LoadAsync(ct);

        var shift = roster.Shifts.FirstOrDefault(s => s.Id == shiftId);
        if (shift is null) return Results.NotFound("Shift not found in roster.");

        await db.Entry(roster).Collection(r => r.Assignments)
            .Query()
            .Where(a => a.RosterShiftId == shiftId)
            .LoadAsync(ct);

        // Roster.Assignments now contains only this shift's assignments
        var alreadyAssigned = roster.Assignments.Count(a => a.Status != AssignmentStatus.Cancelled);
        var slotsNeeded = shift.RequiredHeadcount - alreadyAssigned;

        if (slotsNeeded <= 0)
            return Results.Ok(new { Message = "Shift is already fully staffed.", Created = 0, Skipped = Array.Empty<object>() });

        // Build candidates from provided employee pool (caller specifies who to consider)
        var candidateEmployeeIds = request.CandidateEmployeeIds?.Distinct().ToList()
            ?? await db.EmployeeSkills
                .Where(s => s.TenantId == tenantId && s.IsActive)
                .Select(s => s.EmployeeId)
                .Distinct()
                .ToListAsync(ct);

        // Already assigned employees — exclude (Assignments already filtered to this shift)
        var alreadyAssignedIds = roster.Assignments
            .Where(a => a.Status != AssignmentStatus.Cancelled)
            .Select(a => a.EmployeeId)
            .ToHashSet();

        candidateEmployeeIds = candidateEmployeeIds
            .Where(e => !alreadyAssignedIds.Contains(e))
            .ToList();

        if (candidateEmployeeIds.Count == 0)
            return Results.Ok(new { Message = "No candidate employees to consider.", Created = 0, Skipped = Array.Empty<object>() });

        // 5 batched queries regardless of candidate count — O(1) queries instead of O(N) per-employee loop.
        var contexts = await AssignmentContextLoader.BulkAsync(
            candidateEmployeeIds, tenantId, shift.WorkDate, db, ct);

        // Build SchedulingCandidates for scorer
        var schedulingCandidates = candidateEmployeeIds.Select(empId =>
        {
            var ctx = contexts[empId];
            var hasLeaveConflict = ctx.ApprovedLeaves.Any(l => shift.WorkDate >= l.Start && shift.WorkDate <= l.End);
            return new SchedulingCandidate
            {
                EmployeeId = empId,
                Skills = ctx.Skills,
                IsOnLeave = hasLeaveConflict,
                IsAbsent = false,
                IsUnavailable = false,
                WeeklyHoursWorked = ctx.WeeklyHoursWorked,
                OvertimePolicy = ctx.OvertimePolicy,
                // Use 4-week fairness history so employees who consistently work nights/weekends
                // are deprioritized even if they had none this specific calendar week
                NightShiftsThisWeek = ctx.FairnessHistoryAssignments.Count(w => w.ShiftType == ShiftType.Night),
                WeekendShiftsThisWeek = ctx.FairnessHistoryAssignments.Count(w =>
                    w.WorkDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday),
                ConsecutiveNightShifts = ComputeConsecutiveNightShifts(ctx.WeeklyAssignments, shift.WorkDate),
            };
        }).ToList();

        var fillRequest = new ShiftFillRequest
        {
            RosterShiftId = shift.Id,
            ShiftType = shift.Type,
            WorkDate = shift.WorkDate,
            RequiredSkills = shift.RequiredSkills.ToList(),
        };

        var scores = CandidateScorer.Score(schedulingCandidates, fillRequest);

        var created = new List<Guid>();
        var skipped = new List<object>();

        foreach (var score in scores)
        {
            if (created.Count >= slotsNeeded) break;

            var ctx = contexts[score.EmployeeId];
            var constraintResult = AssignmentConstraintEngine.Evaluate(shift, ctx);

            if (!constraintResult.IsAllowed)
            {
                skipped.Add(new
                {
                    EmployeeId = score.EmployeeId,
                    Reason = constraintResult.Reason,
                    Code = constraintResult.ViolationCode,
                });
                continue;
            }

            try
            {
                var assignment = roster.Assign(score.EmployeeId, shiftId, ctx);
                created.Add(assignment.Id);
            }
            catch (InvalidOperationException ex)
            {
                skipped.Add(new { EmployeeId = score.EmployeeId, Reason = ex.Message, Code = "ASSIGN_FAILED" });
            }
        }

        if (created.Count > 0)
        {
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning("AutoAssign shift {ShiftId} roster {RosterId}: concurrency conflict. {Message}", shiftId, id, ex.Message);
                return Results.Conflict(new
                {
                    Message = "Roster was modified by another request. Reload and retry.",
                    Code = "CONCURRENCY_CONFLICT",
                });
            }
        }

        logger.LogInformation(
            "AutoAssign shift {ShiftId} roster {RosterId}: {Created}/{Needed} slots filled from {Candidates} candidates. {Skipped} skipped.",
            shiftId, id, created.Count, slotsNeeded, candidateEmployeeIds.Count, skipped.Count);

        return Results.Ok(new
        {
            Created = created.Count,
            AssignmentIds = created,
            Skipped = skipped,
            SlotsRemaining = slotsNeeded - created.Count,
        });
    }

    // ── Assignments ───────────────────────────────────────────────────────────────

    private static async Task<IResult> CreateAssignment(
        [FromBody] CreateAssignmentRequest request,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var roster = await db.Rosters
            .FirstOrDefaultAsync(r => r.Id == request.RosterId && r.TenantId == tenantId, ct);
        if (roster is null) return Results.NotFound("Roster not found.");

        await db.Entry(roster).Collection(r => r.Shifts)
            .Query()
            .Where(s => s.Id == request.RosterShiftId)
            .LoadAsync(ct);

        var shift = roster.Shifts.FirstOrDefault(s => s.Id == request.RosterShiftId);
        if (shift is null) return Results.NotFound("Shift not found in roster.");

        await db.Entry(roster).Collection(r => r.Assignments)
            .Query()
            .Where(a => a.RosterShiftId == request.RosterShiftId)
            .LoadAsync(ct);

        var context = await AssignmentContextLoader.SingleAsync(
            request.EmployeeId, tenantId, shift.WorkDate, db, ct);

        try
        {
            var assignment = roster.Assign(request.EmployeeId, request.RosterShiftId, context);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/assignments/{assignment.Id}", new { assignment.Id });
        }
        catch (InvalidOperationException ex)
        {
            return Results.UnprocessableEntity(new { Message = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { Message = "Roster was modified concurrently. Reload and retry.", Code = "CONCURRENCY_CONFLICT" });
        }
    }

    private static async Task<IResult> RemoveAssignment(
        Guid id,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var assignment = await db.RosterShiftAssignments.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (assignment is null) return Results.NotFound();

        var roster = await db.Rosters
            .Include(r => r.Assignments)
            .FirstOrDefaultAsync(r => r.Id == assignment.RosterId && r.TenantId == tenantId, ct);
        if (roster is null) return Results.Forbid();

        roster.Unassign(id);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    // ── Swaps ─────────────────────────────────────────────────────────────────────

    private static async Task<IResult> RequestSwap(
        [FromBody] RequestSwapRequest request,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        var swap = ShiftSwap.Request(
            tenantId, employeeId.Value, request.RequestedWithEmployeeId,
            request.SourceShiftAssignmentId, request.TargetShiftAssignmentId);

        db.ShiftSwaps.Add(swap);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/swaps/{swap.Id}", new { swap.Id });
    }

    private static async Task<IResult> AcceptSwap(
        Guid id,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var swap = await db.ShiftSwaps.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);
        if (swap is null) return Results.NotFound();

        swap.Accept();
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ApproveSwap(
        Guid id,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, managerId) = user.GetTenantAndEmployee();
        if (tenantId is null || managerId is null) return Results.Unauthorized();

        var swap = await db.ShiftSwaps.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);
        if (swap is null) return Results.NotFound();

        // Load shifts and build revalidation contexts
        var sourceAssignment = await db.RosterShiftAssignments
            .FirstOrDefaultAsync(a => a.Id == swap.SourceShiftAssignmentId, ct);
        var targetAssignment = await db.RosterShiftAssignments
            .FirstOrDefaultAsync(a => a.Id == swap.TargetShiftAssignmentId, ct);

        if (sourceAssignment is null || targetAssignment is null)
            return Results.UnprocessableEntity(new { Message = "One or both shift assignments no longer exist." });

        var sourceShift = await db.RosterShifts.FirstOrDefaultAsync(s => s.Id == sourceAssignment.RosterShiftId, ct);
        var targetShift = await db.RosterShifts.FirstOrDefaultAsync(s => s.Id == targetAssignment.RosterShiftId, ct);

        if (sourceShift is null || targetShift is null)
            return Results.UnprocessableEntity(new { Message = "One or both roster shifts no longer exist." });

        var requesterCtx = await AssignmentContextLoader.SingleAsync(
            swap.RequestedByEmployeeId, tenantId, targetShift.WorkDate, db, ct);
        var counterpartyCtx = await AssignmentContextLoader.SingleAsync(
            swap.RequestedWithEmployeeId, tenantId, sourceShift.WorkDate, db, ct);

        try
        {
            swap.Approve(managerId.Value, sourceShift, counterpartyCtx, targetShift, requesterCtx);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.UnprocessableEntity(new { Message = ex.Message });
        }
    }

    private static async Task<IResult> RejectSwap(
        Guid id,
        [FromBody] RejectSwapRequest request,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        var swap = await db.ShiftSwaps.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);
        if (swap is null) return Results.NotFound();

        swap.Reject(employeeId.Value, request.Reason);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> CancelSwap(
        Guid id,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var swap = await db.ShiftSwaps.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);
        if (swap is null) return Results.NotFound();

        swap.Cancel();
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    // ── Coverage Rules ────────────────────────────────────────────────────────────

    private static async Task<IResult> CreateCoverageRule(
        [FromBody] CreateCoverageRuleRequest request,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var rule = CoverageRule.Create(
            tenantId, request.LocationId, request.LocationName,
            request.MinimumStaff, request.MaximumStaff,
            request.SkillMix?.Select(s => new SkillRequirement(s.SkillId, s.SkillName, s.MinimumLevel, s.Count)));

        db.CoverageRules.Add(rule);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/coverage-rules/{rule.Id}", new { rule.Id });
    }

    private static async Task<IResult> ListCoverageRules(
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        [FromQuery] Guid? locationId,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var query = db.CoverageRules.Where(r => r.TenantId == tenantId && r.IsActive);
        if (locationId.HasValue) query = query.Where(r => r.LocationId == locationId.Value);

        return Results.Ok(await query.ToListAsync(ct));
    }

    private static async Task<IResult> DeactivateCoverageRule(
        Guid id,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var rule = await db.CoverageRules.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, ct);
        if (rule is null) return Results.NotFound();

        rule.Deactivate();
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    // ── Employee Skills ───────────────────────────────────────────────────────────

    private static async Task<IResult> GrantEmployeeSkill(
        [FromBody] GrantEmployeeSkillRequest request,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var skill = EmployeeSkill.Grant(
            tenantId, request.EmployeeId, request.SkillId,
            request.SkillName, request.Level, request.ExpiryDate);

        db.EmployeeSkills.Add(skill);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/employee-skills/{skill.Id}", new { skill.Id });
    }

    private static async Task<IResult> GetEmployeeSkills(
        Guid employeeId,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var s = await db.EmployeeSkills
            .Where(s => s.TenantId == tenantId && s.EmployeeId == employeeId && s.IsActive)
            .ToListAsync(ct);

        return Results.Ok(s);
    }

    private static async Task<IResult> RevokeEmployeeSkill(
        Guid id,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var skill = await db.EmployeeSkills.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);
        if (skill is null) return Results.NotFound();

        skill.Revoke();
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    // Context loading delegated to AssignmentContextLoader (testable, 5 queries regardless of employee count).

    // ── Scheduling helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the number of night shifts immediately preceding <paramref name="workDate"/> in reverse
    /// chronological order. Used for fatigue scoring in the auto-assign scorer.
    /// </summary>
    private static int ComputeConsecutiveNightShifts(
        IReadOnlyList<AssignedShiftWindow> weeklyAssignments,
        DateOnly workDate)
    {
        var nightsBeforeWorkDate = weeklyAssignments
            .Where(w => w.ShiftType == ShiftType.Night && w.WorkDate < workDate)
            .OrderByDescending(w => w.WorkDate)
            .ToList();

        int consecutive = 0;
        var expected = workDate.AddDays(-1);
        foreach (var w in nightsBeforeWorkDate)
        {
            if (w.WorkDate == expected)
            {
                consecutive++;
                expected = expected.AddDays(-1);
            }
            else
            {
                break;
            }
        }
        return consecutive;
    }

    // ── Auto-Fill ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fills all under-staffed shifts in the roster using greedy per-shift assignment.
    /// Processes shifts in chronological order. After each assignment, updates in-memory
    /// candidate contexts so same-week rest and overtime constraints are enforced within this fill run.
    /// Executes a single SaveChanges after all shifts are processed.
    /// Known limitation: for rosters spanning multiple weeks, overtime context is accurate for
    /// the reference week only; fairness history is always the 4-week pre-fill window.
    /// </summary>
    private static async Task<IResult> AutoFillRoster(
        Guid id,
        [FromBody] AutoFillRequest request,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        ILogger<Log> logger,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var roster = await db.Rosters
            .Include(r => r.Shifts)
            .Include(r => r.Assignments)
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, ct);
        if (roster is null) return Results.NotFound();

        var sw = System.Diagnostics.Stopwatch.StartNew();

        var unfilledShifts = roster.Shifts
            .Where(s =>
            {
                var active = roster.Assignments.Count(a => a.RosterShiftId == s.Id && a.Status != AssignmentStatus.Cancelled);
                return active < s.RequiredHeadcount;
            })
            .OrderBy(s => s.WorkDate).ThenBy(s => s.StartTime)
            .ToList();

        var maxToProcess = request.MaxShiftsToFill ?? unfilledShifts.Count;
        unfilledShifts = unfilledShifts.Take(maxToProcess).ToList();

        if (unfilledShifts.Count == 0)
            return Results.Ok(new { Message = "All shifts are fully staffed.", ShiftsFilled = 0, TotalAssigned = 0 });

        var candidateIds = request.CandidateEmployeeIds?.Distinct().ToList()
            ?? await db.EmployeeSkills
                .Where(s => s.TenantId == tenantId && s.IsActive)
                .Select(s => s.EmployeeId)
                .Distinct()
                .ToListAsync(ct);

        if (candidateIds.Count == 0)
            return Results.Ok(new { Message = "No candidate employees.", ShiftsFilled = 0, TotalAssigned = 0 });

        // Load contexts once for the first shift's work date. Each assignment updates the in-memory
        // contexts so rest/overtime constraints are correct across shifts in the same week.
        var contexts = await AssignmentContextLoader.BulkAsync(
            candidateIds, tenantId, unfilledShifts[0].WorkDate, db, ct);

        DateOnly loadedForDate = unfilledShifts[0].WorkDate;

        int totalAssigned = 0;
        int totalSkipped = 0;
        var shiftResults = new List<ShiftFillSummary>();

        foreach (var shift in unfilledShifts)
        {
            // Reload contexts when crossing a week boundary — different overtime/rest window.
            // Carry forward in-session additions from the previous week into the new context.
            var shiftWeekStart = shift.WorkDate.AddDays(-(int)shift.WorkDate.DayOfWeek);
            var loadedWeekStart = loadedForDate.AddDays(-(int)loadedForDate.DayOfWeek);
            if (shiftWeekStart != loadedWeekStart)
            {
                var prevContexts = contexts;
                contexts = await AssignmentContextLoader.BulkAsync(candidateIds, tenantId, shift.WorkDate, db, ct);

                // Merge in-session assignments from the prior week's context into the fresh context.
                // These won't be in the DB yet (pre-SaveChanges) but must be visible to constraint evaluation.
                var prevWeekEnd = loadedWeekStart.AddDays(6);
                foreach (var empId in candidateIds)
                {
                    if (!prevContexts.TryGetValue(empId, out var prev)) continue;
                    var sessionAdded = prev.WeeklyAssignments
                        .Where(w => w.WorkDate >= loadedWeekStart && w.WorkDate <= prevWeekEnd)
                        .Except(contexts[empId].WeeklyAssignments)
                        .ToList();
                    if (sessionAdded.Count == 0) continue;
                    var merged = (List<AssignedShiftWindow>)[.. contexts[empId].WeeklyAssignments, .. sessionAdded];
                    contexts[empId] = contexts[empId] with
                    {
                        FairnessHistoryAssignments = [.. contexts[empId].FairnessHistoryAssignments, .. sessionAdded],
                        WeeklyAssignments = merged,
                        WeeklyHoursWorked = AssignmentContextLoader.ComputeWeeklyHoursFromWindows(merged),
                    };
                }
                loadedForDate = shift.WorkDate;
            }

            var alreadyAssigned = roster.Assignments.Count(a =>
                a.RosterShiftId == shift.Id && a.Status != AssignmentStatus.Cancelled);
            var slotsNeeded = shift.RequiredHeadcount - alreadyAssigned;
            if (slotsNeeded <= 0) continue;

            var alreadyAssignedIds = roster.Assignments
                .Where(a => a.RosterShiftId == shift.Id && a.Status != AssignmentStatus.Cancelled)
                .Select(a => a.EmployeeId)
                .ToHashSet();

            var eligibleIds = candidateIds.Where(e => !alreadyAssignedIds.Contains(e)).ToList();
            if (eligibleIds.Count == 0) continue;

            var candidates = eligibleIds.Select(empId =>
            {
                var ctx = contexts[empId];
                return new SchedulingCandidate
                {
                    EmployeeId = empId,
                    Skills = ctx.Skills,
                    IsOnLeave = ctx.ApprovedLeaves.Any(l => shift.WorkDate >= l.Start && shift.WorkDate <= l.End),
                    IsAbsent = false,
                    IsUnavailable = false,
                    WeeklyHoursWorked = ctx.WeeklyHoursWorked,
                    OvertimePolicy = ctx.OvertimePolicy,
                    NightShiftsThisWeek = ctx.FairnessHistoryAssignments.Count(w => w.ShiftType == ShiftType.Night),
                    WeekendShiftsThisWeek = ctx.FairnessHistoryAssignments.Count(w =>
                        w.WorkDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday),
                    ConsecutiveNightShifts = ComputeConsecutiveNightShifts(ctx.WeeklyAssignments, shift.WorkDate),
                };
            }).ToList();

            var fillRequest = new ShiftFillRequest
            {
                RosterShiftId = shift.Id,
                ShiftType = shift.Type,
                WorkDate = shift.WorkDate,
                RequiredSkills = shift.RequiredSkills.ToList(),
            };
            var scores = CandidateScorer.Score(candidates, fillRequest);

            int shiftAssigned = 0;
            foreach (var score in scores)
            {
                if (shiftAssigned >= slotsNeeded) break;

                var ctx = contexts[score.EmployeeId];
                var constraintResult = AssignmentConstraintEngine.Evaluate(shift, ctx);
                if (!constraintResult.IsAllowed) { totalSkipped++; continue; }

                try
                {
                    var assignment = roster.Assign(score.EmployeeId, shift.Id, ctx);
                    shiftAssigned++;
                    totalAssigned++;

                    // Update in-memory context so subsequent shifts in this fill run see this assignment.
                    var newWindow = new AssignedShiftWindow(shift.WorkDate, shift.StartTime, shift.EndTime, shift.Type);
                    var updatedWeekly = (List<AssignedShiftWindow>)[.. ctx.WeeklyAssignments, newWindow];
                    contexts[score.EmployeeId] = ctx with
                    {
                        WeeklyAssignments = updatedWeekly,
                        FairnessHistoryAssignments = [.. ctx.FairnessHistoryAssignments, newWindow],
                        WeeklyHoursWorked = AssignmentContextLoader.ComputeWeeklyHoursFromWindows(updatedWeekly),
                    };
                }
                catch (InvalidOperationException) { totalSkipped++; }
            }

            shiftResults.Add(new ShiftFillSummary(shift.Id, shiftAssigned, slotsNeeded));
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning("AutoFill roster {RosterId}: concurrency conflict. {Message}", id, ex.Message);
            return Results.Conflict(new
            {
                Message = "Roster was modified concurrently during auto-fill. Reload and retry.",
                Code = "CONCURRENCY_CONFLICT",
            });
        }

        sw.Stop();
        logger.LogInformation(
            "AutoFill roster {RosterId}: {Assigned} assignments across {Shifts} shifts from {Candidates} candidates in {Ms}ms. {Skipped} skipped.",
            id, totalAssigned, unfilledShifts.Count, candidateIds.Count, sw.ElapsedMilliseconds, totalSkipped);

        return Results.Ok(new
        {
            ShiftsFilled = shiftResults.Count(s => s.Assigned > 0),
            TotalAssigned = totalAssigned,
            TotalSkipped = totalSkipped,
            Shifts = shiftResults,
        });
    }

    private sealed record ShiftFillSummary(Guid ShiftId, int Assigned, int Needed);

    // ── Employee Availability ─────────────────────────────────────────────────────

    private static async Task<IResult> DeclareAvailability(
        [FromBody] DeclareAvailabilityRequest request,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        try
        {
            var pref = EmployeeAvailabilityPreference.Declare(
                tenantId, request.EmployeeId, request.DayOfWeek, request.From, request.To);
            db.EmployeeAvailabilityPreferences.Add(pref);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/employee-availability/{pref.Id}", new { pref.Id });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { Message = ex.Message });
        }
    }

    private static async Task<IResult> GetAvailability(
        Guid employeeId,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var prefs = await db.EmployeeAvailabilityPreferences
            .Where(a => a.TenantId == tenantId && a.EmployeeId == employeeId && a.IsActive)
            .OrderBy(a => a.DayOfWeek).ThenBy(a => a.From)
            .Select(a => new { a.Id, a.DayOfWeek, a.From, a.To })
            .ToListAsync(ct);

        return Results.Ok(prefs);
    }

    private static async Task<IResult> RevokeAvailability(
        Guid id,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var pref = await db.EmployeeAvailabilityPreferences
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId, ct);
        if (pref is null) return Results.NotFound();

        pref.Revoke();
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}

// ── Request DTOs ─────────────────────────────────────────────────────────────────

public sealed record CreateRosterRequest(string Name, DateOnly StartDate, DateOnly EndDate);

public sealed record CreateShiftRequest(
    string Name,
    ShiftType Type,
    DateOnly WorkDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid LocationId,
    int RequiredHeadcount,
    List<SkillRequirementDto>? RequiredSkills);

public sealed record CreateAssignmentRequest(Guid RosterId, Guid RosterShiftId, Guid EmployeeId);

public sealed record RequestSwapRequest(
    Guid RequestedWithEmployeeId,
    Guid SourceShiftAssignmentId,
    Guid TargetShiftAssignmentId);

public sealed record RejectSwapRequest(string Reason);

public sealed record CreateCoverageRuleRequest(
    Guid LocationId,
    string LocationName,
    int MinimumStaff,
    int MaximumStaff,
    List<SkillRequirementDto>? SkillMix);

public sealed record SkillRequirementDto(Guid SkillId, string SkillName, int MinimumLevel, int Count);

public sealed record GrantEmployeeSkillRequest(
    Guid EmployeeId,
    Guid SkillId,
    string SkillName,
    int Level,
    DateOnly? ExpiryDate);

public sealed record DeclareAvailabilityRequest(
    Guid EmployeeId,
    DayOfWeek DayOfWeek,
    TimeOnly From,
    TimeOnly To);

public sealed record AutoAssignRequest(
    List<Guid>? CandidateEmployeeIds);

public sealed record AutoFillRequest(
    List<Guid>? CandidateEmployeeIds,
    int? MaxShiftsToFill);
