// -----------------------------------------------------------------------
// <copyright file="CoverageFragilityCalculator.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Intelligence.Services.Scoring;

/// <summary>
/// Result of a coverage fragility calculation.
/// </summary>
/// <param name="FragilityScore">Composite fragility score [0–100]. Higher = more fragile.</param>
/// <param name="BurnoutPressureComponent">Burnout-pressure contribution [0–25 pts].</param>
/// <param name="DependencyConcentrationComponent">Critical-shift concentration contribution [0–40 pts].</param>
/// <param name="CriticalExposureComponent">Mean-dependency-risk contribution [0–35 pts].</param>
public sealed record CoverageFragilityResult(
    decimal FragilityScore,
    decimal BurnoutPressureComponent,
    decimal DependencyConcentrationComponent,
    decimal CriticalExposureComponent
);

/// <summary>
/// Computes a site/scope-level coverage fragility score from existing Intelligence data.
///
/// Formula weights:
///   BurnoutPressure         25% (max 25 pts) — high burnout population elevates absence risk
///   DependencyConcentration 40% (max 40 pts) — critical shifts concentrated on few people
///   CriticalExposure        35% (max 35 pts) — mean role irreplaceability across scope
///
/// Zero I/O. Pure static.
/// </summary>
public static class CoverageFragilityCalculator
{
    private const decimal W_Burnout = 0.25m;
    private const decimal W_Concentration = 0.40m;
    private const decimal W_Exposure = 0.35m;

    /// <summary>
    /// Calculates fragility for the given scope inputs.
    /// </summary>
    /// <param name="input">Scope-level aggregates. Must not be null.</param>
    /// <returns>Fragility score and component breakdown.</returns>
    public static CoverageFragilityResult Calculate(CoverageFragilityInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        // BurnoutPressure: fraction at High/Critical × max 100 × weight
        var burnoutPressure = Math.Round(
            Math.Clamp(input.HighCriticalBurnoutPct, 0m, 1m) * 100m * W_Burnout, 1);

        // DependencyConcentration: fraction of scope with majority critical-shift ownership × max 100 × weight
        var dependencyConcentration = Math.Round(
            Math.Clamp(input.CriticalShiftConcentrationRatio, 0m, 1m) * 100m * W_Concentration, 1);

        // CriticalExposure: normalised mean dependency score × weight
        var criticalExposure = Math.Round(
            Math.Clamp(input.MeanDependencyScore, 0m, 100m) * W_Exposure, 1);

        var total = Math.Min(burnoutPressure + dependencyConcentration + criticalExposure, 100m);

        return new CoverageFragilityResult(total, burnoutPressure, dependencyConcentration, criticalExposure);
    }
}
