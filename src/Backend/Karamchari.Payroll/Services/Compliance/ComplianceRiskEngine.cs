using Karamchari.Payroll.Domain.Compliance;

namespace Karamchari.Payroll.Services.Compliance;

public record RiskResult(decimal EstimatedPenalty, string RiskLevel, string Reason);

public interface IComplianceRiskEngine
{
    RiskResult Evaluate(ComplianceFiling filing, decimal amount);
}

/// <summary>
/// Predicts penalties and risks for late compliance filings based on Indian statutory rules.
/// </summary>
public sealed class ComplianceRiskEngine : IComplianceRiskEngine
{
    public RiskResult Evaluate(ComplianceFiling filing, decimal amount)
    {
        if (filing.Status == FilingStatus.Filed)
            return new RiskResult(0, "Safe", "Filing completed on time.");

        var today = DateTime.UtcNow;
        var daysLate = (today - filing.DueDate).Days;

        if (daysLate <= 0)
        {
            var daysRemaining = Math.Abs(daysLate);
            if (daysRemaining <= 3)
                return new RiskResult(0, "Medium", $"Due in {daysRemaining} days.");
            return new RiskResult(0, "Low", "Within deadline.");
        }

        var penalty = filing.Type switch
        {
            ComplianceType.EpfEcr => CalculateEpfPenalty(amount, daysLate),
            ComplianceType.EsicMonthly => CalculateEsicPenalty(amount, daysLate),
            ComplianceType.TdsForm24Q => CalculateTdsPenalty(daysLate),
            _ => 0
        };

        var level = daysLate > 30 ? "Critical" : "High";
        return new RiskResult(penalty, level, $"{daysLate} days overdue.");
    }

    private decimal CalculateEpfPenalty(decimal amount, int daysLate)
    {
        // Interest: 12% p.a.
        var interest = amount * 0.12m * daysLate / 365;

        // Damages: Sliding scale
        var damageRate = daysLate switch
        {
            <= 60 => 0.05m,
            <= 120 => 0.10m,
            <= 180 => 0.15m,
            _ => 0.25m
        };

        var damages = amount * damageRate;

        return Math.Round(interest + damages, 0, MidpointRounding.AwayFromZero);
    }

    private decimal CalculateEsicPenalty(decimal amount, int daysLate)
    {
        // Interest: 12% p.a.
        var interest = amount * 0.12m * daysLate / 365;
        return Math.Round(interest, 0, MidpointRounding.AwayFromZero);
    }

    private decimal CalculateTdsPenalty(int daysLate)
    {
        // ₹200 per day of delay
        return daysLate * 200;
    }
}
