namespace Karamchari.TimeAttendance.Domain.Leaves;

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Defines a leave year boundary. Required because tenants use different year types:
/// Jan-Dec (calendar), Apr-Mar (India financial), Oct-Sep (US fiscal), or joining anniversary.
/// All carry-forward, accrual reset, and expiry calculations are anchored to this.
/// </summary>
public sealed class LeaveYear : AggregateRoot<Guid>, ITenantOwned
{
    private LeaveYear(Guid id, string name, DateOnly startDate, DateOnly endDate,
        LeaveYearType yearType) : base(id)
    {
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        YearType = yearType;
        IsActive = true;
    }

    private LeaveYear()
    {
        TenantId = string.Empty;
        Name = string.Empty;
    }

    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public LeaveYearType YearType { get; private set; }
    public bool IsActive { get; private set; }

    public static LeaveYear Create(string name, DateOnly startDate, DateOnly endDate, LeaveYearType yearType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (endDate <= startDate)
            throw new ArgumentException("End date must be after start date.");
        return new LeaveYear(Guid.NewGuid(), name.Trim(), startDate, endDate, yearType);
    }

    /// <summary>Creates the standard calendar year (Jan 1 → Dec 31).</summary>
    public static LeaveYear CalendarYear(int year)
        => Create($"CY {year}", new DateOnly(year, 1, 1), new DateOnly(year, 12, 31), LeaveYearType.Calendar);

    /// <summary>Creates an Indian financial year (Apr 1 → Mar 31 next year).</summary>
    public static LeaveYear IndiaFinancialYear(int startYear)
        => Create($"FY {startYear}-{startYear + 1 - 2000:D2}",
            new DateOnly(startYear, 4, 1),
            new DateOnly(startYear + 1, 3, 31),
            LeaveYearType.Financial);

    public bool Contains(DateOnly date) => date >= StartDate && date <= EndDate;

    public bool Overlaps(DateOnly from, DateOnly to) => from <= EndDate && to >= StartDate;

    /// <summary>
    /// Returns the portion of a date range that falls within this year.
    /// Used for leave-spanning-year-boundary calculations.
    /// </summary>
    public (DateOnly From, DateOnly To)? Clip(DateOnly from, DateOnly to)
    {
        var clippedFrom = from < StartDate ? StartDate : from;
        var clippedTo = to > EndDate ? EndDate : to;
        if (clippedFrom > clippedTo) return null;
        return (clippedFrom, clippedTo);
    }

    public void Deactivate() => IsActive = false;
}

public enum LeaveYearType
{
    Calendar = 1,
    Financial = 2,
    Custom = 3,
    Anniversary = 4
}
