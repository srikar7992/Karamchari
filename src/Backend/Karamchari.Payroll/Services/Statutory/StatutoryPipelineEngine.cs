namespace Karamchari.Payroll.Services.Statutory;

/// <summary>
/// Orchestrates the execution of statutory rules in a deterministic order.
/// </summary>
public sealed class StatutoryPipelineEngine
{
    /// <summary>
    /// Executes the pipeline for a given context and ruleset.
    /// </summary>
    public static StatutoryExecutionResult Execute(StatutoryContext context, IStatutoryRuleSet ruleSet)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(ruleSet);

        foreach (var rule in ruleSet.Rules.OrderBy(r => r.ExecutionOrder))
        {
            var result = rule.Apply(context);
            if (result.IsApplicable)
            {
                context.ComputedValues[rule.Name] = result.Amount;
            }
        }

        // Net Pay = Monthly Gross - Sum(Statutory Deductions)
        decimal totalDeductions = context.ComputedValues.Values.Sum();
        decimal netPay = context.Breakdown.MonthlyGross - totalDeductions;

        return new StatutoryExecutionResult(netPay, context.ComputedValues);
    }
}

/// <summary>
/// Defines a collection of rules applicable for a specific period.
/// </summary>
public interface IStatutoryRuleSet
{
    /// <summary>Gets the financial year this ruleset covers.</summary>
    FinancialYear Year { get; }
    
    /// <summary>Gets the sorted list of rules.</summary>
    IReadOnlyList<IStatutoryRule> Rules { get; }
}
