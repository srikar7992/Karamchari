// -----------------------------------------------------------------------
// <copyright file="StatutoryAbstractions.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Services.Statutory;

using Karamchari.Payroll.Domain;

/// <summary>
/// Represents a financial year (e.g., 2026-2027).
/// </summary>
public record FinancialYear(int StartYear, int EndYear);

/// <summary>
/// The shared state container that passes through the statutory pipeline.
/// </summary>
public class StatutoryContext
{
    /// <summary>Gets the CTC breakdown results.</summary>
    public CTCBreakdownResult Breakdown { get; }

    /// <summary>Gets the employee's payroll profile.</summary>
    public PayrollProfile Profile { get; }

    /// <summary>Gets the current financial year.</summary>
    public FinancialYear Year { get; }

    /// <summary>Gets the specific month being processed (1-12).</summary>
    public int PayrollMonth { get; }

    /// <summary>Gets the running ledger of deductions applied during this pipeline run.</summary>
    public Dictionary<string, decimal> ComputedValues { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="StatutoryContext"/> class.
    /// </summary>
    public StatutoryContext(CTCBreakdownResult breakdown, PayrollProfile profile, FinancialYear year, int payrollMonth)
    {
        Breakdown = breakdown;
        Profile = profile;
        Year = year;
        PayrollMonth = payrollMonth;
    }

    /// <summary>
    /// Sums specific monthly components (e.g., Basic + DA) to form a wage base.
    /// </summary>
    public decimal GetBaseWage(IEnumerable<Guid> componentIds)
    {
        ArgumentNullException.ThrowIfNull(componentIds);
        return componentIds.Sum(id =>
            Breakdown.MonthlyBreakdown.TryGetValue(id, out var val) ? val : 0m);
    }
}

/// <summary>
/// Common metadata for all statutory rules.
/// </summary>
public interface IStatutoryRuleMetadata
{
    /// <summary>Gets the unique name of the rule (e.g., "EPF_Employee").</summary>
    string Name { get; }

    /// <summary>Gets the execution order relative to other rules.</summary>
    int ExecutionOrder { get; }
}

/// <summary>
/// Defines a single statutory calculation rule.
/// </summary>
public interface IStatutoryRule : IStatutoryRuleMetadata
{
    /// <summary>
    /// Applies the rule to the current context.
    /// </summary>
    StatutoryResult Apply(StatutoryContext context);
}

/// <summary>
/// Defines a statutory calculation rule that requires asynchronous execution (e.g. database lookups).
/// </summary>
public interface IAsyncStatutoryRule : IStatutoryRuleMetadata
{
    /// <summary>
    /// Applies the rule asynchronously.
    /// </summary>
    ValueTask<StatutoryResult> ApplyAsync(StatutoryContext context);
}

/// <summary>
/// The result of applying a single statutory rule.
/// </summary>
/// <param name="RuleName">The name of the rule.</param>
/// <param name="Amount">The calculated deduction amount.</param>
/// <param name="IsApplicable">Whether the rule was applicable for the given context.</param>
/// <param name="Explanation">A human-readable breakdown of how the amount was derived.</param>
public record StatutoryResult(string RuleName, decimal Amount, bool IsApplicable, string Explanation);

/// <summary>
/// The final result of the statutory pipeline execution.
/// </summary>
public record StatutoryExecutionResult(decimal NetPay, Dictionary<string, decimal> Deductions);
