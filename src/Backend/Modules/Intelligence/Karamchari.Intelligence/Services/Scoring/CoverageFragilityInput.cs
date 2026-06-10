// -----------------------------------------------------------------------
// <copyright file="CoverageFragilityInput.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Intelligence.Services.Scoring;

/// <summary>
/// Inputs for <see cref="CoverageFragilityCalculator"/>.
/// All values are derived from existing Intelligence module data; no cross-module signals required.
/// </summary>
/// <param name="HighCriticalBurnoutPct">Fraction of scope employees at High or Critical burnout risk [0–1]. Source: WorkforceHotspot.HighCriticalBurnoutPct / 100.</param>
/// <param name="CriticalShiftConcentrationRatio">Fraction of scope employees with CriticalShiftCoverageRatio &gt; 0.5 (each covers majority of at least one critical shift) [0–1].</param>
/// <param name="MeanDependencyScore">Mean EmployeeDependencyScore across scope employees [0–100].</param>
/// <param name="ScopeEmployeeCount">Total number of employees in the scope used for this computation.</param>
public sealed record CoverageFragilityInput(
    decimal HighCriticalBurnoutPct,
    decimal CriticalShiftConcentrationRatio,
    decimal MeanDependencyScore,
    int ScopeEmployeeCount
);
