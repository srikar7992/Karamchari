// -----------------------------------------------------------------------
// <copyright file="HolidayRecalculationService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Services;

using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Handles retroactive holiday declaration: government declares holiday after leave already approved.
/// Recalculates ActualDays, restores balance difference, and updates calendar entries.
///
/// Scenario: Employee on leave May 10-15. Government declares May 12 as holiday.
/// Result: ActualDays drops by 1. Balance restored by 1.
/// </summary>
public sealed class HolidayRecalculationService(TimeAttendanceDbContext db)
{
    private readonly TimeAttendanceDbContext _db = db;

    public async Task<HolidayRecalculationResult> RecalculateForNewHolidayAsync(
        DateOnly holidayDate,
        CancellationToken ct = default)
    {
        // Find approved leaves that span the new holiday
        List<LeaveRequest> affected = await _db.LeaveRequests
            .Where(r => r.Status == LeaveRequestStatus.Approved
                && r.StartDate <= holidayDate
                && r.EndDate >= holidayDate)
            .ToListAsync(ct);

        var adjustments = new List<HolidayRecalculationAdjustment>();

        foreach (LeaveRequest? request in affected)
        {
            // Skip hourly and half-day — their calculations are different
            if (request.Mode is LeaveMode.Hourly or LeaveMode.HalfDay) continue;

            decimal adjustment = 1.0m;
            request.AdjustActualDaysForHoliday(holidayDate, (double)adjustment);

            // Restore balance
            LeaveBalance? balance = await _db.LeaveBalances
                .Include(b => b.Entries)
                .FirstOrDefaultAsync(b => b.EmployeeId == request.EmployeeId
                    && b.PolicyId == request.PolicyId, ct);

            balance?.Restore(adjustment, holidayDate,
                $"HolidayRecalc:{holidayDate:yyyy-MM-dd}:{request.Id}");

            // Update calendar projection
            LeaveCalendarEntry? calendarEntry = await _db.LeaveCalendarEntries
                .FirstOrDefaultAsync(e => e.LeaveRequestId == request.Id
                    && e.Date == holidayDate, ct);

            if (calendarEntry is not null)
            {
                _db.LeaveCalendarEntries.Remove(calendarEntry);
            }

            adjustments.Add(new HolidayRecalculationAdjustment(
                request.Id, request.EmployeeId, adjustment));
        }

        await _db.SaveChangesAsync(ct);
        return new HolidayRecalculationResult(holidayDate, adjustments);
    }
}

public sealed record HolidayRecalculationResult(
    DateOnly HolidayDate,
    IReadOnlyList<HolidayRecalculationAdjustment> Adjustments)
{
    public int AffectedRequestCount => Adjustments.Count;
    public decimal TotalDaysRestored => Adjustments.Sum(a => a.DaysRestored);
}

public sealed record HolidayRecalculationAdjustment(
    Guid LeaveRequestId,
    Guid EmployeeId,
    decimal DaysRestored);
