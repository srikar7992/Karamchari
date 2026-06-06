namespace Karamchari.Intelligence.Services.Scoring;

/// <summary>
/// Inputs for <see cref="DependencyRiskCalculator"/>.
/// All values sourced from <c>WorkforceSignalRecord</c> rows.
/// </summary>
/// <param name="CriticalShiftOwnershipRatio">Fraction of the tenant's critical shifts where this employee is the sole cover [0–1].</param>
/// <param name="CrossTrainingDepth">Number of distinct other roles this employee can substitute for [0–10+].</param>
/// <param name="UniqueRoleFlag">1 if this employee is the sole practitioner of their role within the team; 0 otherwise.</param>
/// <param name="ApprovalAuthorityIndex">Level of approval authority held exclusively: 0=none, 1=minor, 2=major, 3=exclusive.</param>
public sealed record DependencyRiskInput(
    decimal CriticalShiftOwnershipRatio,
    int CrossTrainingDepth,
    int UniqueRoleFlag,
    int ApprovalAuthorityIndex
);
