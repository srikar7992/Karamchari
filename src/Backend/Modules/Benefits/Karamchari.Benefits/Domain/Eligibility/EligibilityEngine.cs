namespace Karamchari.Benefits.Domain.Eligibility;

public static class EligibilityEngine
{
    public static bool IsEligible(
        string employmentType, int tenureDays, double hoursPerWeek,
        IEnumerable<EligibilityRule> rules)
    {
        foreach (var rule in rules)
            switch (rule.RuleType)
            {
                case EligibilityRuleType.EmploymentType when rule.Value != employmentType:
                    return false;
                case EligibilityRuleType.MinTenureDays when tenureDays < int.Parse(rule.Value):
                    return false;
                case EligibilityRuleType.MinHoursPerWeek when hoursPerWeek < double.Parse(rule.Value):
                    return false;
            }
        return true;
    }
}
