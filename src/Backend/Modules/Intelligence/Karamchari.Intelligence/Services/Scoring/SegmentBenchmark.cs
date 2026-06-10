// -----------------------------------------------------------------------
// <copyright file="SegmentBenchmark.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Intelligence.Domain.Workforce;

namespace Karamchari.Intelligence.Services.Scoring;

/// <summary>
/// Per-segment risk classification thresholds.
/// Replaces fixed global thresholds so that "20 OT hours" is Critical for a Technology
/// segment employee but only Moderate for an EmergencyServices employee.
///
/// Thresholds represent the score at which an employee crosses into that risk tier.
/// </summary>
public static class SegmentBenchmark
{
    /// <summary>
    /// Classifies a burnout score using segment-specific thresholds.
    /// </summary>
    public static WorkforceRiskLevel ClassifyBurnout(decimal score, WorkforceSegment segment)
    {
        var (moderate, high, critical) = BurnoutThresholds(segment);
        return Classify(score, moderate, high, critical);
    }

    /// <summary>
    /// Classifies an attrition score using segment-specific thresholds.
    /// </summary>
    public static WorkforceRiskLevel ClassifyAttrition(decimal score, WorkforceSegment segment)
    {
        var (moderate, high, critical) = AttritionThresholds(segment);
        return Classify(score, moderate, high, critical);
    }

    // ── Threshold tables ──────────────────────────────────────────────────────

    /// <summary>Returns (moderate, high, critical) burnout thresholds for the segment.</summary>
    public static (decimal Moderate, decimal High, decimal Critical) BurnoutThresholds(
        WorkforceSegment segment) => segment switch
        {
            // Technology workers: lower thresholds — burnout detectable earlier, less physical resilience buffer
            WorkforceSegment.Technology => (25m, 55m, 75m),

            // Healthcare: regulated rest rules, slightly elevated tolerance for clinical intensity
            WorkforceSegment.Healthcare => (35m, 65m, 85m),

            // Emergency services: highest tolerance — sustained high-intensity work is the norm
            WorkforceSegment.EmergencyServices => (40m, 70m, 88m),

            // Warehouse: physical work with higher OT norms; slight adjustment
            WorkforceSegment.Warehouse => (35m, 65m, 82m),

            // Retail, General: standard thresholds
            _ => (30m, 60m, 80m)
        };

    /// <summary>Returns (moderate, high, critical) attrition thresholds for the segment.</summary>
    public static (decimal Moderate, decimal High, decimal Critical) AttritionThresholds(
        WorkforceSegment segment) => segment switch
        {
            // Technology: lower threshold — high job mobility makes early attrition signals more predictive
            WorkforceSegment.Technology => (25m, 52m, 72m),

            // EmergencyServices: workforce loyalty and regulation reduces voluntary attrition signals
            WorkforceSegment.EmergencyServices => (35m, 65m, 85m),

            // Healthcare, Warehouse, Retail, General: standard
            _ => (30m, 60m, 80m)
        };

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static WorkforceRiskLevel Classify(
        decimal score, decimal moderate, decimal high, decimal critical) => score switch
        {
            _ when score >= critical => WorkforceRiskLevel.Critical,
            _ when score >= high => WorkforceRiskLevel.High,
            _ when score >= moderate => WorkforceRiskLevel.Moderate,
            _ => WorkforceRiskLevel.Low
        };
}
