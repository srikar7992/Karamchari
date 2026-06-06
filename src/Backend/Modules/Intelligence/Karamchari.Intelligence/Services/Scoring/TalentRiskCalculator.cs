using Karamchari.Intelligence.Domain.Signals;
using Karamchari.Intelligence.Domain.Workforce;

namespace Karamchari.Intelligence.Services.Scoring;

/// <summary>
/// Composite talent risk score: identifies employees who are both high-risk (burnout/attrition)
/// AND high-value (dependency). These are the most dangerous people to lose.
///
/// Formula:
///   base = 40% burnout + 40% attrition + 20% dependency
///   if burnout ≥ 70 AND attrition ≥ 60 → compound penalty +15 pts
///   score = clamp(base + compound, 0, 100)
/// </summary>
public static class TalentRiskCalculator
{
    private const decimal W_Burnout = 0.40m;
    private const decimal W_Attrition = 0.40m;
    private const decimal W_Dependency = 0.20m;
    private const decimal CompoundPenaltyValue = 15m;

    private const decimal BurnoutCompoundThreshold = 70m;
    private const decimal AttritionCompoundThreshold = 60m;

    /// <summary>Calculates a deterministic talent risk score in [0, 100].</summary>
    public static TalentRiskResult Calculate(TalentRiskInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var burnoutComponent = input.BurnoutScore * W_Burnout;
        var attritionComponent = input.AttritionScore * W_Attrition;
        var dependencyComponent = input.CriticalShiftCoverageRatio * 100m * W_Dependency;

        var compound = input.BurnoutScore >= BurnoutCompoundThreshold
                    && input.AttritionScore >= AttritionCompoundThreshold
            ? CompoundPenaltyValue
            : 0m;

        var raw = burnoutComponent + attritionComponent + dependencyComponent + compound;
        var score = Math.Clamp(Math.Round(raw, 1), 0m, 100m);
        var riskLevel = ScoreToRiskLevel(score);

        var contributors = new List<ContributingFactor>
        {
            new("BurnoutRisk", burnoutComponent, EvidenceType.SystemCalculated,
                $"Burnout score {input.BurnoutScore:F0} × 40% weight"),
            new("AttritionRisk", attritionComponent, EvidenceType.SystemCalculated,
                $"Attrition score {input.AttritionScore:F0} × 40% weight"),
            new("DependencyRisk", dependencyComponent, EvidenceType.SystemCalculated,
                $"Critical shift coverage {input.CriticalShiftCoverageRatio:P0} × 20% weight")
        };

        var penalties = compound > 0
            ? new List<PenalizingFactor>
              {
                  new("CompoundBurnoutAttrition", compound,
                      $"Compound penalty: burnout ≥ {BurnoutCompoundThreshold} AND attrition ≥ {AttritionCompoundThreshold}")
              }
            : new List<PenalizingFactor>();

        var summary = $"Talent risk {score:F0}/100 ({riskLevel}). " +
                      $"Burnout {input.BurnoutScore:F0}, Attrition {input.AttritionScore:F0}, " +
                      $"Dependency {input.CriticalShiftCoverageRatio:P0}.";
        if (compound > 0) summary += " Compound penalty applied.";

        var explanation = ScoreExplanation.Compile(
            contributors,
            penalties,
            new List<MissingEvidence>(),
            summary);

        return new TalentRiskResult(
            score, riskLevel, burnoutComponent, attritionComponent, dependencyComponent, compound, explanation);
    }

    public static WorkforceRiskLevel ScoreToRiskLevel(decimal score) => score switch
    {
        < 30m => WorkforceRiskLevel.Low,
        < 60m => WorkforceRiskLevel.Moderate,
        < 80m => WorkforceRiskLevel.High,
        _ => WorkforceRiskLevel.Critical
    };
}

/// <summary>Input for TalentRiskCalculator.</summary>
public sealed record TalentRiskInput(
    Guid EmployeeId,
    decimal BurnoutScore,
    decimal AttritionScore,
    decimal CriticalShiftCoverageRatio);

/// <summary>Output of TalentRiskCalculator.Calculate.</summary>
public sealed record TalentRiskResult(
    decimal Score,
    WorkforceRiskLevel RiskLevel,
    decimal BurnoutComponent,
    decimal AttritionComponent,
    decimal DependencyComponent,
    decimal CompoundPenalty,
    ScoreExplanation Explanation);
