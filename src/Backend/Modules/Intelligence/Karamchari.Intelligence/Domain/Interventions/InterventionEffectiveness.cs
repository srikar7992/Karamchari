// -----------------------------------------------------------------------
// <copyright file="InterventionEffectiveness.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;
using Karamchari.Intelligence.Domain.Workforce;

namespace Karamchari.Intelligence.Domain.Interventions;

/// <summary>
/// Historical effectiveness of an <see cref="InterventionTemplate"/> in the context of
/// a specific <see cref="WorkforceSignalType"/>.
///
/// The same intervention can have very different outcomes per signal context.
/// Example: Compensation Correction → Attrition risk: 42% / Burnout risk: 3%.
///
/// Keyed on (TenantId, TemplateId, SignalType).
/// Recomputed nightly from resolved <see cref="Workforce.InterventionOutcome"/> records.
/// Maps to Intel_InterventionEffectiveness.
/// </summary>
public sealed class InterventionEffectiveness : ITenantOwned
{
    public Guid Id { get; private set; }

    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>The template this effectiveness record belongs to.</summary>
    public Guid TemplateId { get; private set; }

    /// <summary>The signal context in which this template was applied.</summary>
    public WorkforceSignalType SignalType { get; private set; }

    /// <summary>Total number of times this template was applied for this signal type.</summary>
    public int Applications { get; private set; }

    /// <summary>
    /// Fraction of applications with Effective or PartiallyEffective outcome (0..1).
    /// Defaults to 0.5 (neutral) when <see cref="Applications"/> is below the minimum sample.
    /// </summary>
    public decimal SuccessRate { get; private set; }

    /// <summary>Median days from intervention assignment to score improvement.</summary>
    public int MedianOutcomeDays { get; private set; }

    /// <summary>Statistical confidence in the success rate (0..1). Low until sample is adequate.</summary>
    public decimal Confidence { get; private set; }

    public DateTime LastComputedAt { get; private set; }

    private InterventionEffectiveness() { }

    /// <summary>
    /// Creates a fresh effectiveness record with neutral defaults.
    /// Call <see cref="Update"/> once sufficient outcomes are available.
    /// </summary>
    public static InterventionEffectiveness Seed(
        string tenantId,
        Guid templateId,
        WorkforceSignalType signalType)
    {
        return new InterventionEffectiveness
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TemplateId = templateId,
            SignalType = signalType,
            Applications = 0,
            SuccessRate = 0.5m,
            MedianOutcomeDays = 30,
            Confidence = 0m,
            LastComputedAt = DateTime.UtcNow
        };
    }

    public void Update(
        int applications,
        decimal successRate,
        int medianOutcomeDays,
        decimal confidence)
    {
        Applications = applications;
        SuccessRate = Math.Clamp(successRate, 0m, 1m);
        MedianOutcomeDays = Math.Max(0, medianOutcomeDays);
        Confidence = Math.Clamp(confidence, 0m, 1m);
        LastComputedAt = DateTime.UtcNow;
    }
}
