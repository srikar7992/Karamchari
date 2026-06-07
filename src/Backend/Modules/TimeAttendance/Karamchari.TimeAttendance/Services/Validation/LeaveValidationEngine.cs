namespace Karamchari.TimeAttendance.Services.Validation;

/// <summary>
/// Runs all registered validation rules in sequence.
/// Collects all failures — does not short-circuit on first error (show all issues to employee).
/// </summary>
public sealed class LeaveValidationEngine(IEnumerable<ILeaveValidationRule> rules)
{
    private readonly IEnumerable<ILeaveValidationRule> _rules = rules;

    public async Task<LeaveValidationEngineResult> ValidateAsync(
        LeaveValidationContext context,
        CancellationToken ct = default)
    {
        var failures = new List<LeaveValidationResult>();

        foreach (ILeaveValidationRule rule in _rules)
        {
            LeaveValidationResult result = await rule.ValidateAsync(context, ct);
            if (!result.IsValid)
                failures.Add(result);
        }

        return new LeaveValidationEngineResult(failures.Count == 0 || failures.All(f => f.Severity == ValidationSeverity.Warning), failures);
    }
}

public sealed class LeaveValidationEngineResult(bool isValid, IReadOnlyList<LeaveValidationResult> failures)
{
    public bool IsValid { get; } = isValid;
    public IReadOnlyList<LeaveValidationResult> Failures { get; } = failures;
    public IEnumerable<string> ErrorMessages => Failures.Where(f => f.Severity == ValidationSeverity.Error).Select(f => f.Message ?? f.RuleName ?? "Unknown error");
    public IEnumerable<string> Warnings => Failures.Where(f => f.Severity == ValidationSeverity.Warning).Select(f => f.Message ?? f.RuleName ?? "Warning");
}
