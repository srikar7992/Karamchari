// -----------------------------------------------------------------------
// <copyright file="ShiftPremiumRule.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;

namespace Karamchari.Payroll.Domain.WorkRules;

/// <summary>
/// Defines a premium rule triggered by shift type, time-of-day, or calendar condition.
/// Multiple rules compose — an employee may attract Night + Holiday premiums simultaneously.
/// </summary>
public sealed class ShiftPremiumRule : ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public PremiumType PremiumType { get; private set; }
    public decimal PremiumPercentage { get; private set; }

    // Time-of-day window — null for non-time-based premiums (Weekend, Holiday, Hazard, RemoteSite)
    public TimeOnly? WindowStart { get; private set; }
    public TimeOnly? WindowEnd { get; private set; }

    // Calendar condition — null means always applicable when premium type matches
    public bool AppliesToWeekends { get; private set; }
    public bool AppliesToPublicHolidays { get; private set; }

    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private ShiftPremiumRule() { }

    public static ShiftPremiumRule CreateTimeWindow(
        string tenantId,
        string name,
        PremiumType premiumType,
        decimal premiumPercentage,
        TimeOnly windowStart,
        TimeOnly windowEnd)
    {
        if (premiumPercentage <= 0) throw new ArgumentException("Premium percentage must be positive.");

        return new ShiftPremiumRule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            PremiumType = premiumType,
            PremiumPercentage = premiumPercentage,
            WindowStart = windowStart,
            WindowEnd = windowEnd,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public static ShiftPremiumRule CreateCalendar(
        string tenantId,
        string name,
        PremiumType premiumType,
        decimal premiumPercentage,
        bool appliesToWeekends,
        bool appliesToPublicHolidays)
    {
        if (premiumPercentage <= 0) throw new ArgumentException("Premium percentage must be positive.");

        return new ShiftPremiumRule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            PremiumType = premiumType,
            PremiumPercentage = premiumPercentage,
            AppliesToWeekends = appliesToWeekends,
            AppliesToPublicHolidays = appliesToPublicHolidays,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public bool AppliesAt(TimeOnly time)
    {
        if (WindowStart == null || WindowEnd == null) return false;
        // Handles overnight (22:00-06:00)
        if (WindowStart <= WindowEnd)
            return time >= WindowStart && time <= WindowEnd;
        return time >= WindowStart || time <= WindowEnd;
    }

    public bool AppliesOnDate(DateOnly date, bool isPublicHoliday)
    {
        if (AppliesToPublicHolidays && isPublicHoliday) return true;
        if (AppliesToWeekends && (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday))
            return true;
        return false;
    }

    public decimal Calculate(decimal basePay) => basePay * (PremiumPercentage / 100m);
}
