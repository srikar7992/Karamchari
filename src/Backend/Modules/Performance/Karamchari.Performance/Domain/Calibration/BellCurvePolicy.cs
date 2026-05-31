namespace Karamchari.Performance.Domain.Calibration;

/// <summary>
/// Value object representing target bucket distribution for bell-curve calibration.
/// BucketTargetAllocation percentages must sum to 100.
/// </summary>
public sealed record BellCurvePolicy(
    string PolicyName,
    IReadOnlyList<BucketTargetAllocation> BucketTargets)
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static BellCurvePolicy Create(string policyName, IReadOnlyList<BucketTargetAllocation> targets)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        if (targets == null || targets.Count == 0)
            throw new ArgumentException("BellCurvePolicy must have at least one bucket.");

        var total = targets.Sum(t => t.TargetPercent);
        if (Math.Abs(total - 100m) > 0.01m)
            throw new ArgumentException($"Bucket target percentages must sum to 100. Got {total}.");

        return new BellCurvePolicy(policyName.Trim(), targets.ToList().AsReadOnly());
    }
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record BucketTargetAllocation(
    string BucketLabel,
    decimal TargetPercent,
    decimal TolerancePercent);
