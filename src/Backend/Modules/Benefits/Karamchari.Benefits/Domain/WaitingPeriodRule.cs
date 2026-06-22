// -----------------------------------------------------------------------
// <copyright file="WaitingPeriodRule.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Benefits.Domain;

/// <summary>
/// Policy-level model for when an employee becomes benefit-eligible after hire.
/// HR configures a rule per plan; the rule is evaluated at election time against the employee's hire date.
///
/// EF Core persists this via two columns on the BenefitPlans table:
///   WaitingPeriodType  nvarchar(30) — the discriminator (e.g., "Immediate", "FixedDays")
///   WaitingPeriodDays  int          — the numeric parameter (0 when not applicable)
/// Use <see cref="ToColumns"/> and <see cref="FromColumns"/> at the persistence boundary.
/// </summary>
public abstract record WaitingPeriodRule
{
    /// <summary>Returns the first date on which the employee is benefit-eligible.</summary>
    public abstract DateOnly EligibleFrom(DateOnly hireDate);

    /// <summary>Decomposes the rule into its two DB column values.</summary>
    public abstract (string Type, int Days) ToColumns();

    /// <summary>Rehydrates a rule from its stored column values. Throws on unrecognised type strings.</summary>
    public static WaitingPeriodRule FromColumns(string type, int days) =>
        type switch
        {
            nameof(Immediate) => new Immediate(),
            nameof(FixedDays) => new FixedDays(days),
            nameof(FirstOfMonthAfterDays) => new FirstOfMonthAfterDays(days),
            nameof(ProbationComplete) => new ProbationComplete(days),
            _ => throw new ArgumentException($"Unknown WaitingPeriodRule type: '{type}'.", nameof(type))
        };

    // ── Subtypes ───────────────────────────────────────────────────────────────

    /// <summary>Eligible on the hire date — no waiting period. Most common for short-term contracts.</summary>
    public sealed record Immediate : WaitingPeriodRule
    {
        public override DateOnly EligibleFrom(DateOnly hireDate) => hireDate;
        public override (string Type, int Days) ToColumns() => (nameof(Immediate), 0);
    }

    /// <summary>
    /// Eligible exactly <see cref="Days"/> calendar days after hire.
    /// e.g., FixedDays(30) means the employee waits one month before coverage begins.
    /// </summary>
    public sealed record FixedDays(int Days) : WaitingPeriodRule
    {
        public override DateOnly EligibleFrom(DateOnly hireDate) => hireDate.AddDays(Days);
        public override (string Type, int Days) ToColumns() => (nameof(FixedDays), Days);
    }

    /// <summary>
    /// Eligible on the first day of the calendar month that follows the expiry of
    /// <see cref="Days"/> days after hire. Common for plans where coverage is always
    /// aligned to the start of a month (e.g., "30 days + first of next month").
    /// If <c>hireDate.AddDays(Days)</c> falls on the 1st, that date is returned directly.
    /// </summary>
    public sealed record FirstOfMonthAfterDays(int Days) : WaitingPeriodRule
    {
        public override DateOnly EligibleFrom(DateOnly hireDate)
        {
            var afterWait = hireDate.AddDays(Days);
            if (afterWait.Day == 1) return afterWait;
            return new DateOnly(afterWait.Year, afterWait.Month, 1).AddMonths(1);
        }

        public override (string Type, int Days) ToColumns() => (nameof(FirstOfMonthAfterDays), Days);
    }

    /// <summary>
    /// Eligible after completing a probation period of <see cref="ProbationDays"/> days.
    /// Semantically equivalent to <see cref="FixedDays"/> but communicates intent:
    /// eligibility is tied to passing probation, not an arbitrary calendar delay.
    /// </summary>
    public sealed record ProbationComplete(int ProbationDays) : WaitingPeriodRule
    {
        public override DateOnly EligibleFrom(DateOnly hireDate) => hireDate.AddDays(ProbationDays);
        public override (string Type, int Days) ToColumns() => (nameof(ProbationComplete), ProbationDays);
    }
}
