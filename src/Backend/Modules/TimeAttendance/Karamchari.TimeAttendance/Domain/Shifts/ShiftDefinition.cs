// -----------------------------------------------------------------------
// <copyright file="ShiftDefinition.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Shifts;

public enum ShiftCategory
{
    Fixed,      // Fixed start/end times (e.g. 09:00-18:00)
    Flexible,   // Employee works minimum hours per day within a window; no fixed start
    Rotating,   // Cycle-based: week 1 = Morning, week 2 = Night, etc.
    Split,      // Two separate work periods per day (e.g. 09:00-13:00 and 17:00-21:00)
    OnCall      // No schedule; employee responds when paged
}

/// <summary>
/// Aggregate root for a reusable shift template.
/// Defines when work happens, allowed grace periods, and break rules.
/// Supports Fixed, Flexible, Rotating, Split, and OnCall patterns.
/// </summary>
public sealed class ShiftDefinition : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;

    public ShiftCategory Category { get; private set; }

    // Fixed / Rotating / Split (first leg)
    public TimeOnly? StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }

    // Split shift: second work period
    public TimeOnly? SplitStartTime { get; private set; }
    public TimeOnly? SplitEndTime { get; private set; }

    // Flexible shift: window + minimum hours
    public TimeOnly? FlexWindowStart { get; private set; }
    public TimeOnly? FlexWindowEnd { get; private set; }
    public int MinHoursPerDay { get; private set; }

    // Rotating shift: how many calendar days per rotation cycle
    public int RotationCycleDays { get; private set; }

    // Cross-midnight detection — computed
    public bool IsOvernight => StartTime.HasValue && EndTime.HasValue && EndTime < StartTime;

    // Night shift: starts between 20:00 and 06:00 (inclusive window)
    public bool IsNightShift => StartTime.HasValue && (StartTime.Value.Hour >= 20 || StartTime.Value.Hour < 6);

    // Grace periods in minutes
    public int LateArrivalGraceMinutes { get; private set; }
    public int EarlyDepartureGraceMinutes { get; private set; }

    // Auto-break deduction: minutes to deduct automatically when employee doesn't punch break
    // Zero means no auto-deduction (rely on break punches)
    public int AutoBreakDeductionMinutes { get; private set; }
    // Minimum worked minutes before auto-break kicks in (e.g. only deduct after 6h)
    public int AutoBreakTriggerMinutes { get; private set; }

    // Minimum work required to mark "Full Day" vs "Half Day"
    public decimal MinimumHoursForFullDay { get; private set; }
    public decimal MinimumHoursForHalfDay { get; private set; }

    // Night differential pay multiplier (e.g. 1.25 for 25% premium)
    public decimal NightDifferentialMultiplier { get; private set; }

    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private ShiftDefinition() { }

    /// <summary>
    /// Creates a standard fixed shift (e.g. 09:00-18:00).
    /// </summary>
    public static ShiftDefinition CreateFixed(
        string tenantId,
        string name,
        string code,
        TimeOnly start,
        TimeOnly end,
        int lateGrace = 15,
        int earlyGrace = 15,
        int autoBreakDeductionMinutes = 0,
        int autoBreakTriggerMinutes = 360,
        decimal nightDifferentialMultiplier = 1.0m)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(nightDifferentialMultiplier, 1.0m);

        double durationMinutes = end > start
            ? (end - start).TotalMinutes
            : (end - start).TotalMinutes + 1440; // overnight

        return new ShiftDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Code = code,
            Category = ShiftCategory.Fixed,
            StartTime = start,
            EndTime = end,
            LateArrivalGraceMinutes = lateGrace,
            EarlyDepartureGraceMinutes = earlyGrace,
            AutoBreakDeductionMinutes = autoBreakDeductionMinutes,
            AutoBreakTriggerMinutes = autoBreakTriggerMinutes,
            MinimumHoursForFullDay = (decimal)(durationMinutes * 0.9 / 60),
            MinimumHoursForHalfDay = (decimal)(durationMinutes * 0.5 / 60),
            NightDifferentialMultiplier = nightDifferentialMultiplier,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Creates a flexible shift: employee works minHoursPerDay within the allowed window.
    /// No late/early penalties; only MinHoursPerDay compliance checked.
    /// </summary>
    public static ShiftDefinition CreateFlexible(
        string tenantId,
        string name,
        string code,
        TimeOnly windowStart,
        TimeOnly windowEnd,
        int minHoursPerDay,
        int autoBreakDeductionMinutes = 0,
        int autoBreakTriggerMinutes = 360)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(minHoursPerDay, 1);

        return new ShiftDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Code = code,
            Category = ShiftCategory.Flexible,
            FlexWindowStart = windowStart,
            FlexWindowEnd = windowEnd,
            MinHoursPerDay = minHoursPerDay,
            LateArrivalGraceMinutes = 0,
            EarlyDepartureGraceMinutes = 0,
            AutoBreakDeductionMinutes = autoBreakDeductionMinutes,
            AutoBreakTriggerMinutes = autoBreakTriggerMinutes,
            MinimumHoursForFullDay = minHoursPerDay,
            MinimumHoursForHalfDay = minHoursPerDay / 2m,
            NightDifferentialMultiplier = 1.0m,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Creates a rotating shift definition (cycle anchor — actual rotation is in ShiftAssignment).
    /// RotationCycleDays = 14 means a 2-week rotating pattern.
    /// </summary>
    public static ShiftDefinition CreateRotating(
        string tenantId,
        string name,
        string code,
        TimeOnly start,
        TimeOnly end,
        int rotationCycleDays,
        int lateGrace = 10,
        int earlyGrace = 10,
        decimal nightDifferentialMultiplier = 1.0m)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rotationCycleDays, 2);
        ArgumentOutOfRangeException.ThrowIfLessThan(nightDifferentialMultiplier, 1.0m);

        double durationMinutes = end > start
            ? (end - start).TotalMinutes
            : (end - start).TotalMinutes + 1440;

        return new ShiftDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Code = code,
            Category = ShiftCategory.Rotating,
            StartTime = start,
            EndTime = end,
            RotationCycleDays = rotationCycleDays,
            LateArrivalGraceMinutes = lateGrace,
            EarlyDepartureGraceMinutes = earlyGrace,
            MinimumHoursForFullDay = (decimal)(durationMinutes * 0.9 / 60),
            MinimumHoursForHalfDay = (decimal)(durationMinutes * 0.5 / 60),
            NightDifferentialMultiplier = nightDifferentialMultiplier,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Creates a split shift with two separate work periods per day.
    /// Total worked = first leg + second leg; break gap in between is unpaid.
    /// </summary>
    public static ShiftDefinition CreateSplit(
        string tenantId,
        string name,
        string code,
        TimeOnly firstStart,
        TimeOnly firstEnd,
        TimeOnly secondStart,
        TimeOnly secondEnd,
        int lateGrace = 10,
        int earlyGrace = 10)
    {
        if (secondStart <= firstEnd)
            throw new ArgumentException("Split shift second leg must start after first leg ends.");

        double leg1Minutes = (firstEnd - firstStart).TotalMinutes;
        double leg2Minutes = (secondEnd - secondStart).TotalMinutes;
        double totalMinutes = leg1Minutes + leg2Minutes;

        return new ShiftDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Code = code,
            Category = ShiftCategory.Split,
            StartTime = firstStart,
            EndTime = firstEnd,
            SplitStartTime = secondStart,
            SplitEndTime = secondEnd,
            LateArrivalGraceMinutes = lateGrace,
            EarlyDepartureGraceMinutes = earlyGrace,
            MinimumHoursForFullDay = (decimal)(totalMinutes * 0.9 / 60),
            MinimumHoursForHalfDay = (decimal)(totalMinutes * 0.5 / 60),
            NightDifferentialMultiplier = 1.0m,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Creates an on-call shift definition. No fixed times; worked minutes tracked from punch pairs.
    /// </summary>
    public static ShiftDefinition CreateOnCall(
        string tenantId,
        string name,
        string code)
    {
        return new ShiftDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Code = code,
            Category = ShiftCategory.OnCall,
            LateArrivalGraceMinutes = 0,
            EarlyDepartureGraceMinutes = 0,
            MinimumHoursForFullDay = 0,
            MinimumHoursForHalfDay = 0,
            NightDifferentialMultiplier = 1.0m,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    // Preserved for backward-compat callers using the old Create() signature
    public static ShiftDefinition Create(
        string tenantId,
        string name,
        string code,
        TimeOnly start,
        TimeOnly end,
        int lateGrace = 15,
        int earlyGrace = 15)
        => CreateFixed(tenantId, name, code, start, end, lateGrace, earlyGrace);

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public void UpdateGrace(int lateGraceMinutes, int earlyGraceMinutes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(lateGraceMinutes);
        ArgumentOutOfRangeException.ThrowIfNegative(earlyGraceMinutes);
        LateArrivalGraceMinutes = lateGraceMinutes;
        EarlyDepartureGraceMinutes = earlyGraceMinutes;
    }

    public void UpdateNightDifferential(decimal multiplier)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(multiplier, 1.0m);
        NightDifferentialMultiplier = multiplier;
    }
}
