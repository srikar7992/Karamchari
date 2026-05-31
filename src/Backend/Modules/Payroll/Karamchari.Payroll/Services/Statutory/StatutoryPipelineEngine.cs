namespace Karamchari.Payroll.Services.Statutory;

/// <summary>
/// Orchestrates the execution of statutory rules in a deterministic order.
/// </summary>
public sealed class StatutoryPipelineEngine
{
    /// <summary>
    /// Executes the pipeline for a given context and ruleset.
    /// </summary>
    public static async Task<StatutoryExecutionResult> ExecuteAsync(StatutoryContext context, IStatutoryRuleSet ruleSet)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(ruleSet);

        foreach (var ruleMetadata in ruleSet.Rules.OrderBy(r => r.ExecutionOrder))
        {
            StatutoryResult result;
            if (ruleMetadata is IAsyncStatutoryRule asyncRule)
            {
                result = await asyncRule.ApplyAsync(context);
            }
            else if (ruleMetadata is IStatutoryRule syncRule)
            {
                result = syncRule.Apply(context);
            }
            else
            {
                throw new InvalidOperationException($"Unknown rule type: {ruleMetadata.GetType().Name}");
            }

            if (result.IsApplicable)
            {
                context.ComputedValues[result.RuleName] = result.Amount;
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
    IReadOnlyList<IStatutoryRuleMetadata> Rules { get; }
}
