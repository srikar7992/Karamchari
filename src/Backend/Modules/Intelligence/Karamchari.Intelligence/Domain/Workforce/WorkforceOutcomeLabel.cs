// -----------------------------------------------------------------------
// <copyright file="WorkforceOutcomeLabel.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Intelligence.Domain.Workforce;

/// <summary>Terminal outcomes recorded for ML label collection.</summary>
public enum WorkforceOutcome
{
    Termination,
    VoluntaryResignation,
    Transfer,
    BurnoutIntervention,
    MedicalLeave,
    PerformanceImprovement
}

/// <summary>
/// Ground-truth label for a workforce outcome event.
/// Paired with WorkforceFeatureSnapshot rows preceding the event date
/// to build supervised training datasets for future attrition/burnout models.
/// </summary>
public sealed class WorkforceOutcomeLabel : AggregateRoot<Guid>, ITenantOwned
{
    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    public Guid EmployeeId { get; private set; }

    public WorkforceOutcome Outcome { get; private set; }

    public DateOnly OutcomeDate { get; private set; }

    /// <summary>Burnout score on the day the outcome occurred (0 if unknown).</summary>
    public decimal BurnoutScoreAtOutcome { get; private set; }

    /// <summary>Attrition score on the day the outcome occurred (0 if unknown).</summary>
    public decimal AttritionScoreAtOutcome { get; private set; }

    /// <summary>Free-text notes (source system, reviewer, context).</summary>
    public string Notes { get; private set; } = string.Empty;

    public DateTime RecordedAt { get; private set; }

    private WorkforceOutcomeLabel() { }

    public static WorkforceOutcomeLabel Record(
        string tenantId,
        Guid employeeId,
        WorkforceOutcome outcome,
        DateOnly outcomeDate,
        decimal burnoutScoreAtOutcome = 0m,
        decimal attritionScoreAtOutcome = 0m,
        string notes = "")
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            Outcome = outcome,
            OutcomeDate = outcomeDate,
            BurnoutScoreAtOutcome = burnoutScoreAtOutcome,
            AttritionScoreAtOutcome = attritionScoreAtOutcome,
            Notes = notes,
            RecordedAt = DateTime.UtcNow
        };
}
