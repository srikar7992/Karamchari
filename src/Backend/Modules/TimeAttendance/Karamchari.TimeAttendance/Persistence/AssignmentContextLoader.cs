// -----------------------------------------------------------------------
// <copyright file="AssignmentContextLoader.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Domain.Rostering;
using Karamchari.TimeAttendance.Domain.Rostering.Constraints;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.TimeAttendance.Persistence;

/// <summary>
/// Loads <see cref="AssignmentContext"/> from the database.
/// <para>
/// <b>BulkAsync executes exactly 5 queries regardless of how many employees are requested.</b>
/// Use this for any path that evaluates multiple candidates. The per-employee equivalent
/// executes 5 × N queries and collapses at scale (1000 candidates = 5000 queries).
/// </para>
/// </summary>
public static class AssignmentContextLoader
{
    /// <summary>
    /// Loads contexts for <paramref name="employeeIds"/> in 5 batched queries (IN clause per table).
    /// </summary>
    public static async Task<Dictionary<Guid, AssignmentContext>> BulkAsync(
        IReadOnlyList<Guid> employeeIds,
        string tenantId,
        DateOnly workDate,
        TimeAttendanceDbContext db,
        CancellationToken ct = default)
    {
        if (employeeIds.Count == 0) return [];

        DateOnly weekStart = workDate.AddDays(-(int)workDate.DayOfWeek);
        DateOnly weekEnd = weekStart.AddDays(6);
        DateOnly fairnessStart = weekStart.AddDays(-21);

        // Query 1: Leaves covering workDate
        var leavesByEmployee = (await db.LeaveRequests
            .Where(l =>
                l.TenantId == tenantId &&
                employeeIds.Contains(l.EmployeeId) &&
                l.Status == LeaveRequestStatus.Approved &&
                l.StartDate <= workDate && l.EndDate >= workDate)
            .Select(l => new { l.EmployeeId, l.StartDate, l.EndDate })
            .ToListAsync(ct))
            .GroupBy(l => l.EmployeeId)
            .ToDictionary(g => g.Key, g => g.Select(l => new LeaveWindow(l.StartDate, l.EndDate)).ToList());

        // Query 2: Active skills
        var skillsByEmployee = (await db.EmployeeSkills
            .Where(s => s.TenantId == tenantId && employeeIds.Contains(s.EmployeeId) && s.IsActive)
            .ToListAsync(ct))
            .GroupBy(s => s.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Query 3: Weekly assignments (rest period + overtime window)
        var weeklyByEmployee = (await db.RosterShiftAssignments
            .Where(a =>
                employeeIds.Contains(a.EmployeeId) &&
                a.WorkDate >= weekStart && a.WorkDate <= weekEnd &&
                a.Status != AssignmentStatus.Cancelled)
            .Join(db.RosterShifts,
                a => a.RosterShiftId,
                s => s.Id,
                (a, s) => new { a.EmployeeId, a.WorkDate, s.StartTime, s.EndTime, s.Type })
            .ToListAsync(ct))
            .GroupBy(x => x.EmployeeId)
            .ToDictionary(g => g.Key, g =>
                g.Select(x => new AssignedShiftWindow(x.WorkDate, x.StartTime, x.EndTime, x.Type)).ToList());

        // Query 4: Declared availability preferences
        var availabilityByEmployee = (await db.EmployeeAvailabilityPreferences
            .Where(a => a.TenantId == tenantId && employeeIds.Contains(a.EmployeeId) && a.IsActive)
            .Select(a => new { a.EmployeeId, a.DayOfWeek, a.From, a.To })
            .ToListAsync(ct))
            .GroupBy(x => x.EmployeeId)
            .ToDictionary(g => g.Key, g =>
                g.Select(x => new AvailabilityWindow(x.DayOfWeek, x.From, x.To)).ToList());

        // Query 5: Fairness history — 4-week lookback to surface recurring night/weekend patterns
        var fairnessByEmployee = (await db.RosterShiftAssignments
            .Where(a =>
                employeeIds.Contains(a.EmployeeId) &&
                a.WorkDate >= fairnessStart && a.WorkDate <= weekEnd &&
                a.Status != AssignmentStatus.Cancelled)
            .Join(db.RosterShifts,
                a => a.RosterShiftId,
                s => s.Id,
                (a, s) => new { a.EmployeeId, a.WorkDate, s.StartTime, s.EndTime, s.Type })
            .ToListAsync(ct))
            .GroupBy(x => x.EmployeeId)
            .ToDictionary(g => g.Key, g =>
                g.Select(x => new AssignedShiftWindow(x.WorkDate, x.StartTime, x.EndTime, x.Type)).ToList());

        return employeeIds.ToDictionary(empId => empId, empId =>
        {
            List<AssignedShiftWindow> weekly = weeklyByEmployee.TryGetValue(empId, out List<AssignedShiftWindow>? w) ? w : (List<AssignedShiftWindow>)[];
            return new AssignmentContext
            {
                ApprovedLeaves = leavesByEmployee.TryGetValue(empId, out List<LeaveWindow>? l) ? l : [],
                AvailabilityWindows = availabilityByEmployee.TryGetValue(empId, out List<AvailabilityWindow>? av) ? av : [],
                Skills = skillsByEmployee.TryGetValue(empId, out List<EmployeeSkill>? sk) ? sk : [],
                WeeklyAssignments = weekly,
                WeeklyHoursWorked = ComputeWeeklyHoursFromWindows(weekly),
                OvertimePolicy = OvertimePolicy.Default,
                FairnessHistoryAssignments = fairnessByEmployee.TryGetValue(empId, out List<AssignedShiftWindow>? fh) ? fh : [],
            };
        });
    }

    /// <summary>Loads context for a single employee. Delegates to <see cref="BulkAsync"/>.</summary>
    public static async Task<AssignmentContext> SingleAsync(
        Guid employeeId,
        string tenantId,
        DateOnly workDate,
        TimeAttendanceDbContext db,
        CancellationToken ct = default)
    {
        Dictionary<Guid, AssignmentContext> map = await BulkAsync([employeeId], tenantId, workDate, db, ct);
        return map.TryGetValue(employeeId, out AssignmentContext? ctx) ? ctx : new AssignmentContext();
    }

    public static decimal ComputeWeeklyHoursFromWindows(IReadOnlyList<AssignedShiftWindow> windows) =>
        windows.Sum(w =>
        {
            bool isOvernight = w.EndTime < w.StartTime;
            return isOvernight
                ? (decimal)(TimeSpan.FromHours(24) - w.StartTime.ToTimeSpan() + w.EndTime.ToTimeSpan()).TotalHours
                : (decimal)(w.EndTime - w.StartTime).TotalHours;
        });
}
