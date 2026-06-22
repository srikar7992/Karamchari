namespace Karamchari.Benefits.Domain.Plans;

public sealed record BenefitPlanTier(string TierType, decimal MonthlyCost, decimal EmployerContribution);
