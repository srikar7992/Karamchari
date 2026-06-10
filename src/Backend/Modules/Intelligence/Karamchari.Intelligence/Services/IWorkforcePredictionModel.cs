// -----------------------------------------------------------------------
// <copyright file="IWorkforcePredictionModel.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Intelligence.Services;

/// <summary>
/// Abstraction for pluggable ML prediction models (Phase 6.1+).
/// Current implementation: LinearForecastEngine (rule-based).
/// Future implementations: XGBoost, LightGBM.
///
/// Contract is intentionally minimal — models receive a feature vector
/// keyed by signal name and return a probability in [0, 1].
/// </summary>
public interface IWorkforcePredictionModel
{
    /// <summary>Unique model identifier (e.g. "linear-v1", "xgboost-burnout-v3").</summary>
    string ModelId { get; }

    /// <summary>Score type this model predicts: "Burnout" or "Attrition".</summary>
    string ScoreType { get; }

    /// <summary>Semantic version of this model.</summary>
    string ModelVersion { get; }

    /// <summary>
    /// Predicts risk for a single employee.
    /// </summary>
    /// <param name="features">Signal name → value dictionary (matches Intel_FeatureSnapshots columns).</param>
    /// <param name="forecastHorizonDays">How many days ahead the prediction covers.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Predicted risk probability in [0, 1].</returns>
    Task<double> PredictAsync(
        IReadOnlyDictionary<string, double> features,
        int forecastHorizonDays,
        CancellationToken ct = default);
}
