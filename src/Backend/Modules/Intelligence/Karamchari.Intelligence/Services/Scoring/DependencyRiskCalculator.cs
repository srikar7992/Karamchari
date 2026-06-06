using Karamchari.Intelligence.Domain.Workforce;

namespace Karamchari.Intelligence.Services.Scoring;

/// <summary>
/// Calculates how much business-continuity damage would result if an employee departed.
/// Pure static — no I/O, directly unit-testable.
///
/// Signal weights:
///   CriticalShiftOwnership   40%
///   UniqueRole               25%
///   InverseCrossTraining     25%  (depth=0 → max risk; depth≥5 → 0 risk)
///   ApprovalAuthority        10%
/// </summary>
public static class DependencyRiskCalculator
{
    private const decimal W_CriticalShift = 0.40m;
    private const decimal W_UniqueRole = 0.25m;
    private const decimal W_CrossTraining = 0.25m;
    private const decimal W_Approval = 0.10m;

    private const int CrossTrainingSaturationDepth = 5;
    private const int MaxApprovalIndex = 3;

    /// <summary>
    /// Calculates dependency risk score in [0, 100].
    /// Null input throws <see cref="ArgumentNullException"/>.
    /// </summary>
    public static DependencyRiskResult Calculate(DependencyRiskInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var criticalShiftPts = Math.Clamp(input.CriticalShiftOwnershipRatio, 0m, 1m) * 100m * W_CriticalShift;

        var uniqueRolePts = (input.UniqueRoleFlag != 0 ? 100m : 0m) * W_UniqueRole;

        var crossTrainDepth = Math.Max(0, input.CrossTrainingDepth);
        var inverseCrossTrain = Math.Max(0, CrossTrainingSaturationDepth - crossTrainDepth)
                                * (100m / CrossTrainingSaturationDepth);
        var crossTrainPts = inverseCrossTrain * W_CrossTraining;

        var approvalIndex = Math.Clamp(input.ApprovalAuthorityIndex, 0, MaxApprovalIndex);
        var approvalPts = (approvalIndex / (decimal)MaxApprovalIndex) * 100m * W_Approval;

        var rawScore = criticalShiftPts + uniqueRolePts + crossTrainPts + approvalPts;
        var score = Math.Clamp(Math.Round(rawScore, 1), 0m, 100m);
        var riskLevel = ScoreToRiskLevel(score);

        return new DependencyRiskResult(
            Score: score,
            RiskLevel: riskLevel,
            CriticalShiftComponent: Math.Round(criticalShiftPts, 1),
            UniqueRoleComponent: Math.Round(uniqueRolePts, 1),
            CrossTrainingComponent: Math.Round(crossTrainPts, 1),
            ApprovalComponent: Math.Round(approvalPts, 1));
    }

    /// <summary>Risk thresholds match the standard WorkforceRiskLevel scale.</summary>
    public static WorkforceRiskLevel ScoreToRiskLevel(decimal score) => score switch
    {
        < 30m => WorkforceRiskLevel.Low,
        < 60m => WorkforceRiskLevel.Moderate,
        < 80m => WorkforceRiskLevel.High,
        _ => WorkforceRiskLevel.Critical
    };
}

/// <summary>Output of <see cref="DependencyRiskCalculator.Calculate"/>.</summary>
public sealed record DependencyRiskResult(
    decimal Score,
    WorkforceRiskLevel RiskLevel,
    decimal CriticalShiftComponent,
    decimal UniqueRoleComponent,
    decimal CrossTrainingComponent,
    decimal ApprovalComponent);
